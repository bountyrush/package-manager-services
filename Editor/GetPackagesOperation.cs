using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace BountyRush.PackageManagerServices
{
    public class GetPackagesOperation : AsyncOperationBase<UnityEditor.PackageManager.PackageInfo[]>
    {
        #region Fields

        private     ListRequest             m_getPackagesRequest;

        #endregion

        #region Base class methods

        protected override void OnStart()
        {
            base.OnStart();

            m_getPackagesRequest    = Client.List();
        }

        protected override void OnProgress()
        {
            base.OnProgress();

            // check whether request is completed
            if (!m_getPackagesRequest.IsCompleted)
            {
                return;
            }

            // gather results
            var     packageList = new List<UnityEditor.PackageManager.PackageInfo>();
            if (m_getPackagesRequest.Status == StatusCode.Success)
            {
                foreach (var package in m_getPackagesRequest.Result)
                {
                    packageList.Add(package);
                }
            }
            SetCompleted(result: packageList.ToArray());
        }

        #endregion
    }
}