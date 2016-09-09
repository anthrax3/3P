﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using YamuiFramework.Fonts;
using YamuiFramework.Helper;
using YamuiFramework.Themes;

namespace YamuiFramework.Controls.YamuiList {

    public class YamuiFilteredList : YamuiScrollList {

        #region constants

        protected const int MinRowHeight = 20;

        #endregion

        #region public properties

        /// <summary>
        /// Height of each row
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override int RowHeight {
            get { return base.RowHeight.ClampMin(MinRowHeight); }
            set { base.RowHeight = value; }
        }

        /// <summary>
        /// Set this to filter the list with the given text
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string FilterString {
            get { return _filterString; }
            set {
                _filterString = value.ToLower().Trim();
                
                if (_initialItems != null && _nbInitialItems > 0) {

                    // apply the filter on each item to compute internal properties
                    _initialItems.ForEach(data => data.FilterApply(_filterString));

                    ApplyFilterPredicate();
                }
            }
        }

        /// <summary>
        /// Predicate to filter the items, only items meeting the predicate requirements will be displayed (applied in addition to the default string filter)
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual Predicate<FilteredItem> FilterPredicate {
            get { return _filterPredicate; }
            set { _filterPredicate = value; }
        }

        #endregion

        #region private fields

        protected Predicate<FilteredItem> _filterPredicate;

        protected List<FilteredItem> _initialItems;

        protected int _nbInitialItems;

        protected string _filterString = string.Empty;

        private Brush _fillBrush;

        private Pen _framePen;
        
        protected const TextFormatFlags TextFlags = TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding;

        #endregion

        #region Set

        /// <summary>
        /// Constructor to initialize stuff
        /// </summary>
        public YamuiFilteredList() {
            _fillBrush = new SolidBrush(YamuiThemeManager.Current.AutoCompletionHighlightBack);
            _framePen = new Pen(YamuiThemeManager.Current.AutoCompletionHighlightBorder);
        }

        /// <summary>
        /// Set the items that will be displayed in the list
        /// </summary>
        public override void SetItems(List<ListItem> listItems) {
            _initialItems = listItems.Cast<FilteredItem>().ToList();
            _nbInitialItems = _initialItems.Count;

            // we reapply the current filter to this new list
            FilterString = FilterString;
        }

        #endregion

        #region 

        /// <summary>
        /// Filter the list of initial items with the filter predicate and the FilterFullyMatch
        /// </summary>
        protected void ApplyFilterPredicate() {
            if (_initialItems == null || _nbInitialItems == 0)
                return;

            List<ListItem> items;

            // apply filter + sort
            if (FilterPredicate != null)
                items = _initialItems.Where(item => item.FilterFullyMatch && FilterPredicate(item)).OrderBy(data => data.FilterDispertionLevel).Cast<ListItem>().ToList();
            else
                items = _initialItems.Where(item => item.FilterFullyMatch).OrderBy(data => data.FilterDispertionLevel).Cast<ListItem>().ToList();

            // base setItems
            base.SetItems(items);
        }

        #endregion

        #region Draw list


        /// <summary>
        /// Called by default to paint the row if no OnRowPaint is defined
        /// </summary>
        protected override void RowPaint(ListItem item, YamuiListRow row, PaintEventArgs e) {
            var backColor = YamuiThemeManager.Current.MenuBg(row.IsSelected, row.IsHovered, !item.IsDisabled);
            var foreColor = YamuiThemeManager.Current.MenuFg(row.IsSelected, row.IsHovered, !item.IsDisabled);

            // background
            e.Graphics.Clear(backColor);

            // foreground
            // left line
            if (row.IsSelected && !item.IsDisabled) {
                using (SolidBrush b = new SolidBrush(YamuiThemeManager.Current.AccentColor)) {
                    e.Graphics.FillRectangle(b, new Rectangle(0, 0, 3, row.ClientRectangle.Height));
                }
            }

            var textRectangle = new Rectangle(5, 0, row.ClientRectangle.Width - 5, RowHeight);

            // letter highlight
            if (!item.IsDisabled)
                DrawTextHighlighting(e.Graphics, ((FilteredItem) item).FilterMatchedRanges, textRectangle, item.DisplayText, TextFlags);

            // text
            TextRenderer.DrawText(e.Graphics, item.DisplayText, FontManager.GetStandardFont(), textRectangle, foreColor, TextFlags);
        }

        #region Letter(s) highlighting

        /// <summary>
        /// Draw a frame for each range of letters that we matched
        /// </summary>
        protected void DrawTextHighlighting(Graphics g, List<CharacterRange> filterMatchedRanges, Rectangle r, string txt, TextFormatFlags flags) {
            if (filterMatchedRanges != null) {
                foreach (CharacterRange range in filterMatchedRanges) {
                    // Measure the text that comes before our range of letters
                    Size precedingTextSize = Size.Empty;
                    if (range.First > 0) {
                        string precedingText = txt.Substring(0, range.First);
                        precedingTextSize = TextRenderer.MeasureText(g, precedingText, FontManager.GetStandardFont(), r.Size, flags);
                    }

                    // Measure the length of our range of letters
                    string highlightText = txt.Substring(range.First, range.Length);
                    Size textToHighlightSize = TextRenderer.MeasureText(g, highlightText, FontManager.GetStandardFont(), r.Size, flags);
                    float textToHighlightLeft = r.X + precedingTextSize.Width;
                    float textToHighlightTop = r.Top + ((r.Height - textToHighlightSize.Height) / 2);

                    // Draw a filled frame around our range of letters
                    DrawSubstringFrame(g, textToHighlightLeft, textToHighlightTop, textToHighlightSize.Width, textToHighlightSize.Height);
                }
            }
        }

        /// <summary>
        /// Draw an indication around the given letter(s) that shows a text match
        /// </summary>
        protected void DrawSubstringFrame(Graphics g, float x, float y, float width, float height, bool useRoundedRectangle = true) {
            if (useRoundedRectangle) {
                using (GraphicsPath path = Utilities.GetRoundedRect(x, y, width, height, 3.0f)) {
                    if (_fillBrush != null)
                        g.FillPath(_fillBrush, path);
                    if (_framePen != null)
                        g.DrawPath(_framePen, path);
                }
            } else {
                if (_fillBrush != null)
                    g.FillRectangle(_fillBrush, x, y, width, height);
                if (_framePen != null)
                    g.DrawRectangle(_framePen, x, y, width, height);
            }
        }

        #endregion

        #endregion

        #region Utilities

        /// <summary>
        /// Associate the TextChanged event of a text box to this method to filter this list with the input text of the textbox
        /// </summary>
        public void OnTextChangedEvent(object sender, EventArgs eventArgs) {
            var textBox = sender as TextBox;
            if (textBox != null)
                FilterString = textBox.Text;
        }

        #endregion

    }
}