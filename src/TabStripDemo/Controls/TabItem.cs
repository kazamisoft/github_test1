using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;

namespace TabStripDemo.Controls
{
    public class TabItem
    {
        public TabItem() { }

        public TabItem(string text) : this(text, Color.Empty) { }

        public TabItem(string text, Color accentColor)
        {
            Text = text;
            AccentColor = accentColor;
        }

        public string Text { get; set; } = string.Empty;

        /// <summary>스타일 1에서 탭 색. Color.Empty 이면 회색 탭.</summary>
        public Color AccentColor { get; set; } = Color.Empty;

        /// <summary>탭을 선택했을 때 SubMenuBar 에 표시할 항목.</summary>
        public List<SubItem> SubItems { get; } = new List<SubItem>();

        public object Tag { get; set; }
    }

    public class SubItem
    {
        public SubItem() { }

        public SubItem(string title, string caption)
        {
            Title = title;
            Caption = caption;
        }

        public string Title { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;

        /// <summary>지정하면 기본 글리프 대신 이 이미지를 그림(18x18 권장).</summary>
        public Image Image { get; set; }

        public object Tag { get; set; }
    }

    public sealed class TabItemCollection : Collection<TabItem>
    {
        private readonly StyledTabStrip _owner;

        internal TabItemCollection(StyledTabStrip owner)
        {
            _owner = owner;
        }

        public TabItem Add(string text)
        {
            return Add(text, Color.Empty);
        }

        public TabItem Add(string text, Color accentColor)
        {
            var tab = new TabItem(text, accentColor);
            Add(tab);
            return tab;
        }

        protected override void InsertItem(int index, TabItem item)
        {
            base.InsertItem(index, item);
            _owner.OnTabsChanged();
        }

        protected override void SetItem(int index, TabItem item)
        {
            base.SetItem(index, item);
            _owner.OnTabsChanged();
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            _owner.OnTabsChanged();
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            _owner.OnTabsChanged();
        }
    }

    public enum TabStripStyle
    {
        /// <summary>스타일 1: 사선 컬러 탭 + 검색/추가 툴버튼.</summary>
        Slanted,
        /// <summary>스타일 2: 라운드 탭 + 검색 박스.</summary>
        Rounded,
    }

    public enum TabToolButton { Search, Add }

    public sealed class TabToolButtonEventArgs : EventArgs
    {
        public TabToolButtonEventArgs(TabToolButton button)
        {
            Button = button;
        }

        public TabToolButton Button { get; }
    }

    public sealed class SubItemEventArgs : EventArgs
    {
        public SubItemEventArgs(int index, SubItem item)
        {
            Index = index;
            Item = item;
        }

        public int Index { get; }
        public SubItem Item { get; }
    }
}
