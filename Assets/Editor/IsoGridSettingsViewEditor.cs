using Home.View;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(IsoGridSettingsView))]
    public class IsoGridSettingsViewEditor : UnityEditor.Editor
    {
        bool _floorFoldout;
        bool _leftWallFoldout;
        bool _rightWallFoldout;
        bool _floorObjectsFoldout;
        bool _wallObjectsFoldout;
        bool _fragmentedObjectsFoldout;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var view = (IsoGridSettingsView)target;
            var state = view.IsoGridStateRef;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("IsoGridState (Runtime)", EditorStyles.boldLabel);

            if (state == null || state.FloorCells == null)
            {
                EditorGUILayout.HelpBox("IsoGridStateは未初期化です（実行中のみ表示されます）", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("GridWidth", state.GridWidth.ToString());
            EditorGUILayout.LabelField("GridHeight", state.GridHeight.ToString());
            EditorGUILayout.LabelField("WallHeight", state.WallHeight.ToString());

            // 床グリッド
            _floorFoldout = EditorGUILayout.Foldout(_floorFoldout, $"FloorCells [{state.GridWidth} x {state.GridHeight}]");
            if (_floorFoldout)
            {
                DrawCellGrid(state.FloorCells, state.GridWidth, state.GridHeight);
            }

            // 左壁グリッド
            _leftWallFoldout = EditorGUILayout.Foldout(_leftWallFoldout, $"LeftWallCells [{state.GridHeight} x {state.WallHeight}]");
            if (_leftWallFoldout)
            {
                DrawCellGrid(state.LeftWallCells,
                    state.LeftWallCells.GetLength(0), state.LeftWallCells.GetLength(1));
            }

            // 右壁グリッド
            _rightWallFoldout = EditorGUILayout.Foldout(_rightWallFoldout, $"RightWallCells [{state.GridWidth} x {state.WallHeight}]");
            if (_rightWallFoldout)
            {
                DrawCellGrid(state.RightWallCells,
                    state.RightWallCells.GetLength(0), state.RightWallCells.GetLength(1));
            }

            // 床オブジェクト配置
            _floorObjectsFoldout = EditorGUILayout.Foldout(_floorObjectsFoldout,
                $"ObjectFootprintStartPositions [{state.ObjectFootprintStartPositions.Count}]");
            if (_floorObjectsFoldout)
            {
                EditorGUI.indentLevel++;
                foreach (var kvp in state.ObjectFootprintStartPositions)
                {
                    EditorGUILayout.LabelField($"UserFurnitureID: {kvp.Key}", $"Position: {kvp.Value}");
                }
                EditorGUI.indentLevel--;
            }

            // 壁オブジェクト配置
            _wallObjectsFoldout = EditorGUILayout.Foldout(_wallObjectsFoldout,
                $"WallObjectFootprintStartPositions [{state.WallObjectFootprintStartPositions.Count}]");
            if (_wallObjectsFoldout)
            {
                EditorGUI.indentLevel++;
                foreach (var kvp in state.WallObjectFootprintStartPositions)
                {
                    EditorGUILayout.LabelField($"UserFurnitureID: {kvp.Key}",
                        $"{kvp.Value.Side} ({kvp.Value.Position})");
                }
                EditorGUI.indentLevel--;
            }

            // FragmentedGridオブジェクト配置
            _fragmentedObjectsFoldout = EditorGUILayout.Foldout(_fragmentedObjectsFoldout,
                $"FragmentedGridObjectPositions [{state.FragmentedGridObjectPositions.Count}]");
            if (_fragmentedObjectsFoldout)
            {
                EditorGUI.indentLevel++;
                foreach (var kvp in state.FragmentedGridObjectPositions)
                {
                    EditorGUILayout.LabelField($"親家具ID: {kvp.Key}", $"子要素数: {kvp.Value.Count}");
                    EditorGUI.indentLevel++;
                    foreach (var child in kvp.Value)
                    {
                        EditorGUILayout.LabelField($"子家具ID: {child.Key}",
                            $"Position: {child.Value.Position}, Depth: {child.Value.Depth}");
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }

            // 実行中に定期的に更新
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        static void DrawCellGrid(Home.State.IsoGridCell[,] cells, int width, int height)
        {
            EditorGUI.indentLevel++;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (!cells[x, y].IsOccupied) continue;
                    EditorGUILayout.LabelField($"[{x}, {y}]", $"UserFurnitureID: {cells[x, y].UserFurnitureId}");
                }
            }
            EditorGUI.indentLevel--;
        }
    }
}
