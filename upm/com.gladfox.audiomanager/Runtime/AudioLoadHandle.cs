using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AudioManagement
{
    public enum AudioLoadStatus
    {
        None = 0,
        Loading = 1,
        Succeeded = 2,
        Failed = 3
    }

    public sealed class AudioLoadHandle
    {
        private readonly IReadOnlyList<AsyncOperationHandle> operations;
        private readonly AsyncOperationHandle<IList<AsyncOperationHandle>> groupOperation;
        private readonly bool completedWithoutOperations;
        private readonly string failReason;

        private AudioLoadHandle(IReadOnlyList<AsyncOperationHandle> operations, AsyncOperationHandle<IList<AsyncOperationHandle>> groupOperation, bool completedWithoutOperations, string failReason)
        {
            this.operations = operations;
            this.groupOperation = groupOperation;
            this.completedWithoutOperations = completedWithoutOperations;
            this.failReason = failReason;
        }

        public static AudioLoadHandle Completed() => new AudioLoadHandle(null, default, true, null);

        public static AudioLoadHandle Failed(string error) => new AudioLoadHandle(null, default, false, error);

        public static AudioLoadHandle FromOperations(IReadOnlyList<AsyncOperationHandle> operations, AsyncOperationHandle<IList<AsyncOperationHandle>> groupOperation)
        {
            return new AudioLoadHandle(operations, groupOperation, false, null);
        }

        public bool IsValid => completedWithoutOperations || !string.IsNullOrEmpty(failReason) || (operations != null && operations.Count > 0) || groupOperation.IsValid();

        public bool IsDone
        {
            get
            {
                if (completedWithoutOperations || !string.IsNullOrEmpty(failReason))
                {
                    return true;
                }

                if (groupOperation.IsValid())
                {
                    return groupOperation.IsDone;
                }

                if (operations == null || operations.Count == 0)
                {
                    return true;
                }

                for (var i = 0; i < operations.Count; i++)
                {
                    if (!operations[i].IsDone)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public float Progress
        {
            get
            {
                if (completedWithoutOperations || !string.IsNullOrEmpty(failReason))
                {
                    return 1f;
                }

                if (groupOperation.IsValid())
                {
                    return groupOperation.PercentComplete;
                }

                if (operations == null || operations.Count == 0)
                {
                    return 1f;
                }

                var total = 0f;
                for (var i = 0; i < operations.Count; i++)
                {
                    total += operations[i].PercentComplete;
                }

                return total / operations.Count;
            }
        }

        public AudioLoadStatus Status
        {
            get
            {
                if (!string.IsNullOrEmpty(failReason))
                {
                    return AudioLoadStatus.Failed;
                }

                if (completedWithoutOperations)
                {
                    return AudioLoadStatus.Succeeded;
                }

                if (!IsDone)
                {
                    return AudioLoadStatus.Loading;
                }

                if (groupOperation.IsValid())
                {
                    return groupOperation.Status == AsyncOperationStatus.Succeeded ? AudioLoadStatus.Succeeded : AudioLoadStatus.Failed;
                }

                if (operations == null || operations.Count == 0)
                {
                    return AudioLoadStatus.Succeeded;
                }

                for (var i = 0; i < operations.Count; i++)
                {
                    if (operations[i].Status != AsyncOperationStatus.Succeeded)
                    {
                        return AudioLoadStatus.Failed;
                    }
                }

                return AudioLoadStatus.Succeeded;
            }
        }

        public string Error
        {
            get
            {
                if (!string.IsNullOrEmpty(failReason))
                {
                    return failReason;
                }

                if (Status != AudioLoadStatus.Failed)
                {
                    return null;
                }

                if (groupOperation.IsValid() && groupOperation.OperationException != null)
                {
                    return groupOperation.OperationException.Message;
                }

                if (operations != null)
                {
                    for (var i = 0; i < operations.Count; i++)
                    {
                        if (operations[i].OperationException != null)
                        {
                            return operations[i].OperationException.Message;
                        }
                    }
                }

                return "Unknown audio load failure.";
            }
        }
    }
}
