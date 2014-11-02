namespace KinectDrummer
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using Microsoft.Kinect;

    class MainWindowViewModel : INotifyPropertyChanged
    {

        //add fields here to be managed by properties below. these should be private
        private bool canStart;
        private bool canStop;

        //add properties here for data binding in XAML. these should be public
        public bool CanStart
        {
            get
            {
                return this.canStart;
            }

            set
            {
                if (this.canStart != value)
                {
                    this.canStart = value;
                    this.OnNotifyPropertyChange("CanStart");
                }
            }
        }

        public bool CanStop
        {
            get
            {
                return this.canStop;
            }

            set
            {
                if (this.canStop != value)
                {
                    this.canStop = value;
                    this.OnNotifyPropertyChange("CanStop");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnNotifyPropertyChange(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
