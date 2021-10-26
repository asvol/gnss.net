﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Asv.Gnss
{
    public class AsyncReaderWriterLock
    {
        private readonly Task<Releaser> _mReaderReleaser;
        private readonly Task<Releaser> _mWriterReleaser;
        private readonly Queue<TaskCompletionSource<Releaser>> _mWaitingWriters =
            new Queue<TaskCompletionSource<Releaser>>();
        private TaskCompletionSource<Releaser> _mWaitingReader =
            new TaskCompletionSource<Releaser>();
        private int _mReadersWaiting;
        private int _mStatus;

        public AsyncReaderWriterLock()
        {
            _mReaderReleaser = Task.FromResult(new Releaser(this, false));
            _mWriterReleaser = Task.FromResult(new Releaser(this, true));
        }

        public Task<Releaser> ReaderLockAsync()
        {
            lock (_mWaitingWriters)
            {
                if (_mStatus >= 0 && _mWaitingWriters.Count == 0)
                {
                    ++_mStatus;
                    return _mReaderReleaser;
                }
                else
                {
                    ++_mReadersWaiting;
                    return _mWaitingReader.Task.ContinueWith(t => t.Result);
                }
            }
        }
        public Task<Releaser> WriterLockAsync()
        {
            lock (_mWaitingWriters)
            {
                if (_mStatus == 0)
                {
                    _mStatus = -1;
                    return _mWriterReleaser;
                }
                else
                {
                    var waiter = new TaskCompletionSource<Releaser>();
                    _mWaitingWriters.Enqueue(waiter);
                    return waiter.Task;
                }
            }
        }
        private void ReaderRelease()
        {
            TaskCompletionSource<Releaser> toWake = null;

            lock (_mWaitingWriters)
            {
                --_mStatus;
                if (_mStatus == 0 && _mWaitingWriters.Count > 0)
                {
                    _mStatus = -1;
                    toWake = _mWaitingWriters.Dequeue();
                }
            }

            if (toWake != null)
                toWake.SetResult(new Releaser(this, true));
        }

        private void WriterRelease()
        {
            TaskCompletionSource<Releaser> toWake = null;
            bool toWakeIsWriter = false;

            lock (_mWaitingWriters)
            {
                if (_mWaitingWriters.Count > 0)
                {
                    toWake = _mWaitingWriters.Dequeue();
                    toWakeIsWriter = true;
                }
                else if (_mReadersWaiting > 0)
                {
                    toWake = _mWaitingReader;
                    _mStatus = _mReadersWaiting;
                    _mReadersWaiting = 0;
                    _mWaitingReader = new TaskCompletionSource<Releaser>();
                }
                else _mStatus = 0;
            }

            if (toWake != null)
                toWake.SetResult(new Releaser(this, toWakeIsWriter));
        }

        public struct Releaser : IDisposable
        {
            private readonly AsyncReaderWriterLock _mToRelease;
            private readonly bool _mWriter;

            internal Releaser(AsyncReaderWriterLock toRelease, bool writer)
            {
                _mToRelease = toRelease;
                _mWriter = writer;
            }

            public void Dispose()
            {
                if (_mToRelease != null)
                {
                    if (_mWriter) _mToRelease.WriterRelease();
                    else _mToRelease.ReaderRelease();
                }
            }
        }

    }
    
}