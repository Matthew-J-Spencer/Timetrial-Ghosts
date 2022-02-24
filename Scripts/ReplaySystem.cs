using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// ReSharper disable IteratorNeverReturns

namespace TarodevGhost {
    public class ReplaySystem {
        private readonly WaitForFixedUpdate _wait = new WaitForFixedUpdate();

        public ReplaySystem(MonoBehaviour runner) {
            runner.StartCoroutine(FixedUpdate());
            runner.StartCoroutine(Update());
        }

        private IEnumerator FixedUpdate() {
            while (true) {
                yield return _wait;
                AddSnapshot();
                _elapsedRecordingTime += Time.smoothDeltaTime;
            }
        }

        private IEnumerator Update() {
            while (true) {
                yield return null;
                _replaySmoothedTime += Time.smoothDeltaTime;
                UpdateReplay();
            }
        }

        #region Recording

        private readonly Dictionary<RecordingType, Recording> _runs = new Dictionary<RecordingType, Recording>();
        private Recording _currentRun;
        private float _elapsedRecordingTime;
        private int _snapshotEveryNFrames;
        private int _frameCount;
        private float _maxRecordingTimeLimit;

        /// <summary>
        /// Begin recording a run
        /// </summary>
        /// <param name="target">The transform you wish to record</param>
        /// <param name="snapshotEveryNFrames">The accuracy of the recording. Smaller number == higher file size</param>
        /// <param name="maxRecordingTimeLimit">Stop recording beyond this time</param>
        public void StartRun(Transform target, int snapshotEveryNFrames = 2, float maxRecordingTimeLimit = 60) {
            _currentRun = new Recording(target);

            _elapsedRecordingTime = 0;

            _snapshotEveryNFrames = Mathf.Max(1, snapshotEveryNFrames);
            _frameCount = 0;

            _maxRecordingTimeLimit = maxRecordingTimeLimit;
        }

        private void AddSnapshot() {
            if (_currentRun == null) return;

            // Capture frame, taking into account the frame skip
            if (_frameCount++ % _snapshotEveryNFrames == 0) _currentRun.AddSnapshot(_elapsedRecordingTime);

            // End a run over the limit
            if (_currentRun.Duration >= _maxRecordingTimeLimit) FinishRun();
        }

        /// <summary>
        /// Complete the current recording
        /// </summary>
        /// <param name="save">If we want to save this run. Use false for restarts</param>
        /// <returns>Whether this run was the fastest so far</returns>
        public bool FinishRun(bool save = true) {
            if (_currentRun == null) return false;
            if (!save) {
                _currentRun = null;
                return false;
            }

            _runs[RecordingType.Last] = _currentRun;
            _currentRun = null;

            if (!GetRun(RecordingType.Best, out var best) || _runs[RecordingType.Last].Duration <= best.Duration) {
                _runs[RecordingType.Best] = _runs[RecordingType.Last];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Set the saved run. This can be pulled from leaderboards or friends, etc
        /// </summary>
        /// <param name="run">The run you'd like to set for playback</param>
        public void SetSavedRun(Recording run) => _runs[RecordingType.Saved] = run;

        /// <summary>
        /// Retrieve a run
        /// </summary>
        /// <param name="type">The type of run you'd like to retrieve</param>
        /// <param name="run">The resulting run</param>
        /// <returns></returns>
        public bool GetRun(RecordingType type, out Recording run) {
            return _runs.TryGetValue(type, out run);
        }

        #endregion

        #region Play Ghost

        private Recording _currentReplay;
        private GameObject _ghostObj;
        private bool _destroyOnComplete;
        private float _replaySmoothedTime;

        /// <summary>
        /// Begin playing a recording
        /// </summary>
        /// <param name="type">The type of recording you wish to play</param>
        /// <param name="ghostObj">The visual representation of the ghost. Must be pre-instantiated (this allows customization)</param>
        /// <param name="destroyOnCompletion">Whether or not to automatically destroy the ghost object when the run completes</param>
        public void PlayRecording(RecordingType type, GameObject ghostObj, bool destroyOnCompletion = true) {
            if (_ghostObj != null) Object.Destroy(_ghostObj);

            if (!GetRun(type, out _currentReplay)) {
                Object.Destroy(ghostObj);
                return;
            }

            _replaySmoothedTime = 0;
            _destroyOnComplete = destroyOnCompletion;

            if (_currentReplay != null) _ghostObj = ghostObj;
            else if (_destroyOnComplete) Object.Destroy(_ghostObj);
        }

        private void UpdateReplay() {
            if (_currentReplay == null) return;

            // Evaluate the point at the current time
            var pose = _currentReplay.EvaluatePoint(_replaySmoothedTime);
            _ghostObj.transform.SetPositionAndRotation(pose.position, pose.rotation);

            // Destroy the replay when done
            if (_replaySmoothedTime > _currentReplay.Duration) {
                _currentReplay = null;
                if (_destroyOnComplete) Object.Destroy(_ghostObj);
            }
        }

        /// <summary>
        /// Stop the replay. Should be called when the player finishes the run before the ghost
        /// </summary>
        public void StopReplay() {
            if (_ghostObj != null) Object.Destroy(_ghostObj);
            _currentReplay = null;
        }

        #endregion
    }

    public enum RecordingType {
        Last = 0,
        Best = 1,
        Saved = 2
    }
}