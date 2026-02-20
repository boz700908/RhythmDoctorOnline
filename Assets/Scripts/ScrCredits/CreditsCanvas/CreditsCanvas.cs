using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

namespace Credits
{
    /// <summary>
    /// 80x24 character buffer; writes to buffer then FlushToTMP builds one rich-text string for a single TMP. Port of CLIRender Canvas.
    /// Logical coordinates: (0..39, 0..23) for set_char (each cell 2 chars); set_string uses same grid, 1 char per logical column.
    /// </summary>
    public class CreditsCanvas
    {
        public const int Width = 80;
        public const int Height = 24;
        public const int LogicalWidth = 40;

        private readonly struct Cell
        {
            public readonly char Char;
            public readonly CanvasColor Color;

            public Cell(char c, CanvasColor col)
            {
                Char = c;
                Color = col;
            }
        }

        private readonly Cell[,] _buffer;
        private readonly CanvasColor _clearColor;
        public int EditsThisFrame { get; private set; }

        public CreditsCanvas(CanvasColor? clearColor = null)
        {
            _buffer = new Cell[Height, Width];
            _clearColor = clearColor ?? CanvasColor.Black;
            ClearLayer(0);
        }

        /// <summary>
        /// Set one logical cell (2 chars) at logical (x,y). x in [0,39], y in [0,23].
        /// </summary>
        public void SetChar(int layer, Vector2Int location, string twoChars, CanvasColor color)
        {
            int x = Mathf.Clamp(location.x, 0, LogicalWidth - 1);
            int y = Mathf.Clamp(location.y, 0, Height - 1);
            int col = x * 2;
            if (col >= Width) return;
            if (twoChars != null && twoChars.Length >= 1)
                SetCell(y, col, twoChars[0], color);
            if (twoChars != null && twoChars.Length >= 2 && col + 1 < Width)
                SetCell(y, col + 1, twoChars[1], color);
            EditsThisFrame++;
        }

        /// <summary>
        /// Set string at logical (x,y). Each character occupies one column; newline moves to next row; wrap at Width.
        /// </summary>
        public void SetString(int layer, Vector2Int location, string str, CanvasColor color)
        {
            if (string.IsNullOrEmpty(str)) return;
            int col = Mathf.Clamp(location.x, 0, LogicalWidth - 1) * 2;
            int row = Mathf.Clamp(location.y, 0, Height - 1);
            int startCol = col;
            foreach (char c in str)
            {
                if (c == '\n')
                {
                    row++;
                    col = startCol;
                    if (row >= Height) break;
                    continue;
                }
                if (col >= Width) { col = 0; row++; if (row >= Height) break; }
                SetCell(row, col, c, color);
                col++;
            }
            EditsThisFrame++;
        }

        private void SetCell(int row, int col, char c, CanvasColor color)
        {
            if (row >= 0 && row < Height && col >= 0 && col < Width)
                _buffer[row, col] = new Cell(c, color);
        }

        public void ClearLayer(int layer)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    _buffer[y, x] = new Cell(' ', _clearColor);
            EditsThisFrame = 0;
        }

        /// <summary>
        /// Build one rich-text string from the buffer and assign to the given TMP. Call each beat after all generators ran.
        /// </summary>
        public void FlushToTMP(TextMeshProUGUI tmp)
        {
            if (tmp == null) return;

            var sb = new StringBuilder(Height * (Width + 20));
            CanvasColor? lastColor = null;

            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    var cell = _buffer[row, col];
                    if (cell.Color.Color != (lastColor?.Color ?? default) || cell.Color.Bright != (lastColor?.Bright ?? false))
                    {
                        if (lastColor != null) sb.Append("</color>");
                        sb.Append(cell.Color.ToTmpRichText());
                        lastColor = cell.Color;
                    }
                    sb.Append(cell.Char);
                }
                if (lastColor != null) { sb.Append("</color>"); lastColor = null; }
                if (row < Height - 1) sb.Append('\n');
            }

            if (lastColor != null) sb.Append("</color>");

            tmp.text = sb.ToString();
            EditsThisFrame = 0;
        }

        /// <summary>
        /// 按行输出 (字符串, 颜色) 片段，供 IMGUI 在 OnGUI 里逐段绘制，解决排版问题。
        /// segmentsByLine 长度至少为 Height，每行会先 Clear 再填入该行的片段。
        /// </summary>
        public void BuildImguiSegments(List<(string, Color)>[] segmentsByLine)
        {
            if (segmentsByLine == null || segmentsByLine.Length < Height) return;

            for (int row = 0; row < Height; row++)
            {
                var list = segmentsByLine[row];
                list.Clear();
                var sb = new StringBuilder(Width);
                CanvasColor prevCol = _buffer[row, 0].Color;
                sb.Append(_buffer[row, 0].Char);

                for (int col = 1; col < Width; col++)
                {
                    var cell = _buffer[row, col];
                    if (cell.Color.Color != prevCol.Color || cell.Color.Bright != prevCol.Bright)
                    {
                        list.Add((sb.ToString(), prevCol.ToUnityColor()));
                        sb.Clear();
                        prevCol = cell.Color;
                    }
                    sb.Append(cell.Char);
                }
                if (sb.Length > 0)
                    list.Add((sb.ToString(), prevCol.ToUnityColor()));
            }
            EditsThisFrame = 0;
        }
    }
}
