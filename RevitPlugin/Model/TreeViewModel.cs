using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPlugin.Model
{
    public class TreeViewModel : INotifyPropertyChanged
    {
        public TreeViewModel(string name, object data)
        {
            Name = name;
            DataSource = data;
            Children = new List<TreeViewModel>();
        }

        #region Properties

        public string Name { get; private set; }
        public object DataSource { get; private set; }
        public List<TreeViewModel> Children { get; private set; }
        public bool IsInitiallySelected { get; private set; }

        bool? isChecked = false;
        TreeViewModel parent;

        #region IsChecked

        public bool? IsChecked
        {
            get { return isChecked; }
            set { SetIsChecked(value, true, true); }
        }

        void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
        {
            if (value == isChecked) return;

            isChecked = value;

            if (updateChildren && isChecked.HasValue && Children != null) Children.ForEach(c => c.SetIsChecked(isChecked, true, false));

            if (updateParent && parent != null) parent.VerifyCheckedState();

            NotifyPropertyChanged("IsChecked");
        }

        void VerifyCheckedState()
        {
            bool? state = null;

            for (int i = 0; i < Children.Count; ++i)
            {
                bool? current = Children[i].IsChecked;
                if (i == 0)
                {
                    state = current;
                }
                else if (state != current)
                {
                    state = null;
                    break;
                }
            }

            SetIsChecked(state, false, true);
        }

        #endregion

        #endregion

        public void Initialize()
        {
            foreach (TreeViewModel child in Children)
            {
                child.parent = this;
                child.Initialize();
            }
        }

        public static List<string> GetTree()
        {
            List<string> selected = new List<string>();


            return selected;
        }

        #region INotifyPropertyChanged Members

        void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
