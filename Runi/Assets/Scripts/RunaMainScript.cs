using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Runa
{
    public class RunaMainScript : MonoBehaviour
    {
        public enum GameStatus
        {
            Menu,
            Playing,
            Paused,
        }

        public enum RotationAxes
        {
            MouseXAndY = 0,
            MouseX     = 1,
            MouseY     = 2
        }

        [Header("Map properties")] public List<GameObject> tilesObstacle;
        public                            List<GameObject> tilesWithoutObstacle;
        public                            float            mapScrollSpeed;
        private                           List<GameObject> _boardRows;
        private                           uint             _rowGeneratedCount;
        private                           bool             _shouldGenerateRow;

        [Header("Camera properties")]
        [SerializeField]
        private Transform _cameraTransform;

        public Transform CameraTransform
        {
            get { return _cameraTransform; }
            set
            {
                _cameraTransform = value;
                if (_cameraTransform)
                    _originalRotation = _cameraTransform.localRotation;
            }
        }

        public  RotationAxes axes         = RotationAxes.MouseXAndY;
        public  float        sensitivityX = 15F;
        public  float        sensitivityY = 15F;
        public  float        minimumX     = -360F;
        public  float        maximumX     = 360F;
        public  float        minimumY     = -60F;
        public  float        maximumY     = 60F;
        public  float        frameCounter = 20;
        private float        _rotationX;
        private float        _rotationY;
        private List<float>  _rotArrayX = new List<float>();
        private float        _rotAverageX;
        private List<float>  _rotArrayY = new List<float>();
        private float        _rotAverageY;
        private Quaternion   _originalRotation;

        [Header("Player properties")] public GameObject          playerPrefab;
        public                               float               playerGravity = 50f;
        public                               float               playerSpeed   = 6.0f;
        private                              GameObject          _playerInstance;
        private                              CharacterController _playerCharacterController;

        [Header("Game properties")] public GameStatus gameStatus;
        public                             byte       tilePerRow   = 5;
        public                             byte       tileMaxDepth = 5;

        private void Start()
        {
            _boardRows = new List<GameObject>();
            InitializeBase();
            _playerInstance = Instantiate(playerPrefab);
            if (_playerInstance)
            {
                _playerCharacterController = _playerInstance.GetComponent<CharacterController>();
                CameraTransform = _playerInstance.GetComponentInChildren<Camera>().transform;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            //Updating map
            if (_shouldGenerateRow)
                GenerateMapRow();
            ScrollMapRows();

            //Updating camera
            UpdateCameraView();

            //Updating player
            UpdatePlayerMovement();
        }

        private void InitializeBase()
        {
            for (var i = 0; i < tileMaxDepth; ++i)
            {
                var rowGo = GenerateMapRow();
                rowGo.transform.Translate(0, 0, -tileMaxDepth);
            }
        }

        private GameObject GenerateMapRow()
        {
            int noObstacleCount = 0, withObstacleCount = 0;
            var rowGo = new GameObject("Row" + _rowGeneratedCount);
            rowGo.transform.position = new Vector3(0, 0, _boardRows.Count > 0 ? _boardRows[_boardRows.Count - 1].transform.position.z + 1 : tileMaxDepth);
            for (var i = 0; i < tilePerRow; ++i)
            {
                var isNotObstacle = Random.Range(0f, 1f) >= 0.3f || i < tilePerRow - 1 && noObstacleCount == 0;
                var tileList = isNotObstacle ? tilesWithoutObstacle : tilesObstacle;
                var tileGo = Instantiate(tileList[Random.Range(0, tileList.Count)], rowGo.transform);
                tileGo.transform.localPosition = new Vector3(i - (tilePerRow % 2 != 0 ? Mathf.Floor((float)tilePerRow / 2) : (float)tilePerRow / 2 - 0.5f),
                                                             0,
                                                             0);
                if (isNotObstacle)
                    noObstacleCount += 1;
                else
                    withObstacleCount += 1;
            }
            _boardRows.Add(rowGo);
            _rowGeneratedCount += 1;
            _shouldGenerateRow = false;

            return rowGo;
        }

        private void ScrollMapRows()
        {
            for (var i = 0; i < _boardRows.Count; i++)
            {
                var boardRow = _boardRows[i];
                boardRow.transform.Translate(0, 0, -mapScrollSpeed * Time.deltaTime);
                if (boardRow.transform.localPosition.z <= -2)
                {
                    Destroy(boardRow);
                    _boardRows.RemoveAt(i);
                    i -= 1;
                }
                else if (i == _boardRows.Count - 1 && boardRow.transform.localPosition.z < tileMaxDepth - 1)
                    _shouldGenerateRow = true;
            }
        }

        private void UpdateCameraView()
        {
            if (!CameraTransform) return;
            if (axes == RotationAxes.MouseXAndY)
            {
                _rotAverageY = 0f;
                _rotAverageX = 0f;
                _rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
                _rotationX += Input.GetAxis("Mouse X") * sensitivityX;
                _rotArrayY.Add(_rotationY);
                _rotArrayX.Add(_rotationX);
                if (_rotArrayY.Count >= frameCounter)
                    _rotArrayY.RemoveAt(0);
                if (_rotArrayX.Count >= frameCounter)
                    _rotArrayX.RemoveAt(0);
                for (int j = 0; j < _rotArrayY.Count; j++)
                    _rotAverageY += _rotArrayY[j];
                for (int i = 0; i < _rotArrayX.Count; i++)
                    _rotAverageX += _rotArrayX[i];
                _rotAverageY /= _rotArrayY.Count;
                _rotAverageX /= _rotArrayX.Count;
                _rotAverageY = ClampAngle(_rotAverageY, minimumY, maximumY);
                _rotAverageX = ClampAngle(_rotAverageX, minimumX, maximumX);
                Quaternion yQuaternion = Quaternion.AngleAxis(_rotAverageY, Vector3.left);
                Quaternion xQuaternion = Quaternion.AngleAxis(_rotAverageX, Vector3.up);
                _playerInstance.transform.localRotation = _originalRotation * xQuaternion * yQuaternion;
            }
            else if (axes == RotationAxes.MouseX)
            {
                _rotAverageX = 0f;
                _rotationX += Input.GetAxis("Mouse X") * sensitivityX;
                _rotArrayX.Add(_rotationX);
                if (_rotArrayX.Count >= frameCounter)
                    _rotArrayX.RemoveAt(0);
                for (int i = 0; i < _rotArrayX.Count; i++)
                    _rotAverageX += _rotArrayX[i];
                _rotAverageX /= _rotArrayX.Count;
                _rotAverageX = ClampAngle(_rotAverageX, minimumX, maximumX);
                Quaternion xQuaternion = Quaternion.AngleAxis(_rotAverageX, Vector3.up);
                _playerInstance.transform.localRotation = _originalRotation * xQuaternion;
            }
            else
            {
                _rotAverageY = 0f;
                _rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
                _rotArrayY.Add(_rotationY);
                if (_rotArrayY.Count >= frameCounter)
                    _rotArrayY.RemoveAt(0);
                for (int j = 0; j < _rotArrayY.Count; j++)
                    _rotAverageY += _rotArrayY[j];
                _rotAverageY /= _rotArrayY.Count;
                _rotAverageY = ClampAngle(_rotAverageY, minimumY, maximumY);
                Quaternion yQuaternion = Quaternion.AngleAxis(_rotAverageY, Vector3.left);
                _playerInstance.transform.localRotation = _originalRotation * yQuaternion;
            }
        }

        public static float ClampAngle(float angle, float min, float max)
        {
            angle = angle % 360;
            if ((angle >= -360F) && (angle <= 360F))
            {
                if (angle < -360F)
                    angle += 360F;
                if (angle > 360F)
                    angle -= 360F;
            }
            return Mathf.Clamp(angle, min, max);
        }

        private void UpdatePlayerMovement()
        {
            if (_playerCharacterController.isGrounded)
            {
                _playerCharacterController.SimpleMove(_playerInstance.transform.right * Input.GetAxis("Horizontal") * playerSpeed);
                _playerCharacterController.SimpleMove(_playerInstance.transform.forward * Input.GetAxis("Vertical") * playerSpeed);
                _playerCharacterController.Move(new Vector3(0, 0, -mapScrollSpeed * Time.deltaTime));
            }
            _playerCharacterController.Move(new Vector3(0, -playerGravity * Time.deltaTime, 0));
        }
    }
}