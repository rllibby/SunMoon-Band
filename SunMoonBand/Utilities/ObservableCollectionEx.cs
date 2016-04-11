/*
 *  Copyright © 2015 Russell Libby
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SunMoonBand.Utilities
{
    public class ObservableCollectionEx<T> : ObservableCollection<T>
    {
        #region Private fields

        private bool _suppressNotification;

        #endregion

        #region Protected methods

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification) base.OnCollectionChanged(e);
        }

        #endregion

        #region Public methods

        public void AddRange(IEnumerable<T> list)
        {
            if (list == null) throw new ArgumentNullException("list");

            _suppressNotification = true;

            try
            {
                Clear();

                foreach (var item in list)
                {
                    Add(item);
                }
            }
            finally
            {
                _suppressNotification = false;
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        #endregion
    }
}
