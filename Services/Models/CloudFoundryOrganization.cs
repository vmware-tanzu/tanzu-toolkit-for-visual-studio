using System.ComponentModel;

namespace TanzuForVS.Services.Models
{
    public class CloudFoundryOrganization : INotifyPropertyChanged
    {
        private string orgName;
        public event PropertyChangedEventHandler PropertyChanged;


        public CloudFoundryOrganization(string orgName)
        {
            OrgName = orgName;
        }

        public string OrgName
        {
            get => orgName;
            set
            {
                orgName = value;
                this.RaisePropertyChangedEvent("OrgName");
            }
        }
        protected void RaisePropertyChangedEvent(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
