/*
 *  Copyright © 2015 Russell Libby
 */
using System;
using System.Threading;

namespace SunMoonBandCommon
{
    /// <summary>
    /// Class for managing the sync mutex and providing automatic cleanup.
    /// </summary>
    public sealed class AppSyncLock : IDisposable
    {
        #region Private fields

        private Mutex _syncLock = new Mutex(false, Common.MutexName);
        private bool _locked;
        private bool _disposed;

        #endregion

        #region Private methods

        /// <summary>
        /// Internal method to perform cleanup on the mutex.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (!disposing || _disposed) return;

            if (_syncLock != null)
            {
                try
                {
                    if (_locked) _syncLock.ReleaseMutex();

                    _syncLock.Dispose();
                    _syncLock = null;
                }
                catch (Exception)
                {
                    _syncLock = null;
                }
                
                _locked = false;
            }

            _disposed = true;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="timeout">The time to wait for mutex aquisition.</param>
        public AppSyncLock(int timeout)
        {
            try
            {
                _locked = _syncLock.WaitOne(timeout);
            }
            catch (AbandonedMutexException)
            {
                _locked = true;
            }
            catch (Exception)
            {
                _locked = false;
            }
        }

        #endregion

        #region Destructor

        /// <summary>
        /// Destructor.
        /// </summary>
        ~AppSyncLock()
        {
            Dispose(false);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Cleanup resources.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Returns true if the lock was aquired, otherwise false.
        /// </summary>
        public bool IsLocked
        {
            get { return _locked; }    
        }

        #endregion
    }
}
