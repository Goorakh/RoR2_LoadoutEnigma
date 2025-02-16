using System;
using System.Diagnostics;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace LoadoutEnigma.Utilities.Extensions
{
    public static class AssetLoadExtensions
    {
        public static void CallOnSuccess<T>(this AsyncOperationHandle<T> handle, Action<T> onSuccess)
        {
#if DEBUG
            StackTrace stackTrace = new StackTrace();
#endif

            handle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    Log.Error($"Failed to load asset '{handle.LocationName}'"
#if DEBUG
                        + $". at {stackTrace}"
#endif
                        );

                    return;
                }

                onSuccess(handle.Result);
            };
        }
    }
}
