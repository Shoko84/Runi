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

        [Header("Map properties")] public List<GameObject> tilesObstacle;
        public                            List<GameObject> tilesWithoutObstacle;
        public                            float            mapScrollSpeed;
        public                            float            rowSpawningDepth = 5f;
        private                           List<GameObject> _boardRows;
        private                           uint             _rowGeneratedCount;
        private                           bool             _shouldGenerateRow;

        [Header("Game properties")] public GameStatus gameStatus;
        public                             byte       tilePerRow = 5;

        private void Start()
        {
            _boardRows = new List<GameObject>();
            _shouldGenerateRow = true;
        }

        private void Update()
        {
            if (_shouldGenerateRow)
                GenerateMapRow();
            ScrollMapRows();
        }

        private void GenerateMapRow()
        {
            int noObstacleCount = 0, withObstacleCount = 0;
            var rowGo = new GameObject("Row" + _rowGeneratedCount);
            rowGo.transform.position = new Vector3(0, 0, _boardRows.Count > 0 ? _boardRows[_boardRows.Count - 1].transform.position.z + 1 : rowSpawningDepth);
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
                else if (i == _boardRows.Count - 1 && boardRow.transform.localPosition.z < rowSpawningDepth - 1)
                    _shouldGenerateRow = true;
            }
        }
    }
}