using System.ComponentModel;

namespace TanzuForVS.Services.Models
{
    public class CloudFoundryInstance
    {
        private string name;
        public event PropertyChangedEventHandler PropertyChanged;

        public CloudFoundryInstance(string name)
        {
            this.Name = name;
        }

        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                this.name = value;
                this.RaisePropertyChangedEvent("Name");
            }
        }

        protected void RaisePropertyChangedEvent(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
