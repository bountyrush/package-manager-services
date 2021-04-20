using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BountyRush.PackageManagerServices
{
    public class AsyncOperationBase<TResult> 
    {
        #region Properties

        public bool IsDone { get; private set; }

        public TResult Result { get; private set; }

        public string Error { get; private set; }

        #endregion

        #region Events

        public event System.Action<AsyncOperationBase<TResult>> OnComplete;

        #endregion

        #region Constructors

        protected AsyncOperationBase()
        {
            // set properties
            Result      = default(TResult);
            Error       = null;
            IsDone      = false;

            // register for editor update
            EditorApplication.delayCall    += OnStartInternal;
        }

        #endregion

        #region Lifecycle methods

        protected virtual void OnStart()
        { }

        protected virtual void OnProgress()
        { }

        protected virtual void OnEnd()
        { }
         
        #endregion

        #region Private methods

        private void OnStartInternal()
        {
            EditorApplication.update    += OnProgressInternal;

            OnStart();
        }

        private void OnProgressInternal()
        {
            OnProgress();
        }

        protected void SetCompleted(TResult result = default(TResult), string error = null)
        {
            // update state
            EditorApplication.update    -= OnProgressInternal;
            IsDone      = true;
            Result      = result;
            Error       = error;

            // invoke finish handler
            OnEnd();

            // send event
            OnComplete?.Invoke(this);
        }

        #endregion
    }
}