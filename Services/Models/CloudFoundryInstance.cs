using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace TanzuForVS.Services.Models
{
    public class CloudFoundryInstance : INotifyPropertyChanged
    {
        private string name;
        private ObservableCollection<CloudFoundryOrganization> orgs;
        public event PropertyChangedEventHandler PropertyChanged;

        public CloudFoundryInstance(string name)
        {
            throw new NotImplementedException();
            //this.InstanceName = name;
            //orgs = new ObservableCollection<CloudFoundryOrganization>(new List<CloudFoundryOrganization>
            //{
            //    new CloudFoundryOrganization("fake org 1"),
            //    new CloudFoundryOrganization("fake org 2"),
            //    new CloudFoundryOrganization("fake org 3")
            //});
        }

        public string InstanceName
        {
            get
            {
                throw new NotImplementedException();

                //return this.name;
            }

            set
            {
                throw new NotImplementedException();

                //this.name = value;
                //this.RaisePropertyChangedEvent("InstanceName");
            }
        }

        public ObservableCollection<CloudFoundryOrganization> Orgs
        {
            get
            {
                return orgs;
            }

            set
            {
                this.orgs = value;
                this.RaisePropertyChangedEvent("Orgs");
            }
        }

        protected void RaisePropertyChangedEvent(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
