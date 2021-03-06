﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.QuickInject
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public class ContainerControlledLifetimeManager : ContextInvariantLifetimeManager, IRequiresRecovery, IDisposable
    {
        private readonly object lockObj = new object();
        private object value;

        public override object GetValue()
        {
            var currentValue = this.value;
            if (currentValue != null)
            {
                return currentValue;
            }

            return this.SynchronizedGetValue();
        }

        public override void SetValue(object newValue)
        {
            Volatile.Write(ref this.value, newValue);
            this.TryExit();
        }

        public override void RemoveValue()
        {
            this.Dispose();
        }

        public void Recover()
        {
            this.TryExit();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.value != null)
            {
                var disposable = this.value as IDisposable;
                disposable?.Dispose();

                this.value = null;
            }
        }

        private void TryExit()
        {
            // Prevent first chance exception when abandoning a lock that has not been entered
            if (Monitor.IsEntered(this.lockObj))
            {
                try
                {
                    Monitor.Exit(this.lockObj);
                }
                catch (SynchronizationLockException)
                {
                    // Noop here - we don't hold the lock and that's ok.
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private object SynchronizedGetValue()
        {
            Monitor.Enter(this.lockObj);
            var result = Volatile.Read<object>(ref this.value);

            if (result != null)
            {
                Monitor.Exit(this.lockObj);
            }

            return result;
        }
    }
}