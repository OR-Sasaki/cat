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

            if (state == null || state.Floor == null)
            {
                EditorGUILayout.HelpBox("IsoGridStateは未初期化です（実行中のみ表示されます）", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("GridWidth", state.Floor.Size.x.ToString());
            EditorGUILayout.LabelField("GridHeight", state.Floor.Size.y.ToString());
            EditorGUILayout.LabelField("WallHeight", state.LeftWall.Size.y.ToString());

            // 床グリッド
            _floorFoldout = EditorGUILayout.Foldout(_floorFoldout, $"FloorCells [{state.Floor.Size.x} x {state.Floor.Size.y}]");
            if (_floorFoldout)
            {
                DrawCellGrid(state.Floor.Cells, state.Floor.Size.x, state.Floor.Size.y);
            }

            // 左壁グリッド
            _leftWallFoldout = EditorGUILayout.Foldout(_leftWallFoldout, $"LeftWallCells [{state.LeftWall.Size.x} x {state.LeftWall.Size.y}]");
            if (_leftWallFoldout)
            {
                DrawCellGrid(state.LeftWall.Cells, state.LeftWall.Size.x, state.LeftWall.Size.y);
            }

            // 右壁グリッド
            _rightWallFoldout = EditorGUILayout.Foldout(_rightWallFoldout, $"RightWallCells [{state.RightWall.Size.x} x {state.RightWall.Size.y}]");
            if (_rightWallFoldout)
            {
                DrawCellGrid(state.RightWall.Cells, state.RightWall.Size.x, state.RightWall.Size.y);
            }

            // 床オブジェクト配置
            _floorObjectsFoldout = EditorGUILayout.Foldout(_floorObjectsFoldout,
                $"Floor.ObjectPositions [{state.Floor.ObjectPositions.Count}]");
            if (_floorObjectsFoldout)
            {
                EditorGUI.indentLevel++;
                foreach (var kvp in state.Floor.ObjectPositions)
                {
                    EditorGUILayout.LabelField($"UserFurnitureID: {kvp.Key}", $"Position: {kvp.Value.Position}");
                }
                EditorGUI.indentLevel--;
            }

            // 壁オブジェクト配置（LeftWall）
            _wallObjectsFoldout = EditorGUILayout.Foldout(_wallObjectsFoldout,
                $"WallObjectPositions [L:{state.LeftWall.ObjectPositions.Count} R:{state.RightWall.ObjectPositions.Count}]");
            if (_wallObjectsFoldout)
            {
                EditorGUI.indentLevel++;
                foreach (var kvp in state.LeftWall.ObjectPositions)
                {
                    EditorGUILayout.LabelField($"UserFurnitureID: {kvp.Key}",
                        $"Left ({kvp.Value.Position})");
                }
                foreach (var kvp in state.RightWall.ObjectPositions)
                {
                    EditorGUILayout.LabelField($"UserFurnitureID: {kvp.Key}",
                        $"Right ({kvp.Value.Position})");
                }
                EditorGUI.indentLevel--;
            }

            // FragmentedGridオブジェクト配置
            _fragmentedObjectsFoldout = EditorGUILayout.Foldout(_fragmentedObjectsFoldout,
                $"FragmentedGrids [{state.FragmentedGridsV2.Count}]");
            if (_fragmentedObjectsFoldout)
            {
                EditorGUI.indentLevel++;
                foreach (var kvp in state.FragmentedGridsV2)
                {
                    var entry = kvp.Value;
                    EditorGUILayout.LabelField($"親家具ID: {kvp.Key}",
                        $"Size: {entry.Size}, 子要素数: {entry.ObjectPositions.Count}");
                    EditorGUI.indentLevel++;
                    foreach (var child in entry.ObjectPositions)
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

        static void DrawCellGrid(int[,] cells, int width, int height)
        {
            // 各セルの表示幅を揃えるため、最大桁数を求める
            var maxLen = 1;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (cells[x, y] == 0) continue;
                    var len = cells[x, y].ToString().Length;
                    if (len > maxLen) maxLen = len;
                }
            }

            var sb = new System.Text.StringBuilder();
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var cellValue = cells[x, y];
                    var text = cellValue != 0 ? cellValue.ToString() : ".";
                    sb.Append(text.PadLeft(maxLen));
                    if (x < width - 1) sb.Append(' ');
                }
                if (y < height - 1) sb.AppendLine();
            }

            var style = new GUIStyle(EditorStyles.textArea)
            {
                font = Font.CreateDynamicFontFromOSFont("Courier New", 11),
                wordWrap = false,
            };
            EditorGUILayout.TextArea(sb.ToString(), style);
        }
    }
}
