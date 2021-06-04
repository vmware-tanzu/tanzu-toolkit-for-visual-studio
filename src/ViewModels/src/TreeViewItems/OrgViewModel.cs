﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;
using Tanzu.Toolkit.Services.ErrorDialog;

namespace Tanzu.Toolkit.ViewModels
{
    public class OrgViewModel : TreeViewItemViewModel
    {
        /* ERROR MESSAGE CONSTANTS */
        internal static readonly string _emptySpacesPlaceholderMsg = "No spaces";
        internal static readonly string _loadingMsg = "Loading spaces...";
        internal static readonly string _getSpacesFailureMsg = "Unable to load spaces.";
        private static IErrorDialog _dialogService;

        public CloudFoundryOrganization Org { get; }

        public OrgViewModel(CloudFoundryOrganization org, IServiceProvider services)
            : base(null, services)
        {
            _dialogService = services.GetRequiredService<IErrorDialog>();

            Org = org;
            DisplayText = Org.OrgName;

            LoadingPlaceholder = new PlaceholderViewModel(parent: this, services)
            {
                DisplayText = _loadingMsg,
            };

            EmptyPlaceholder = new PlaceholderViewModel(parent: this, Services)
            {
                DisplayText = _emptySpacesPlaceholderMsg,
            };
        }

        public async Task<ObservableCollection<SpaceViewModel>> FetchChildren()
        {
            var newSpacesList = new ObservableCollection<SpaceViewModel>();

            var spacesResponse = await CloudFoundryService.GetSpacesForOrgAsync(Org);

            if (spacesResponse.Succeeded)
            {
                var spaces = new ObservableCollection<CloudFoundrySpace>(spacesResponse.Content);

                foreach (CloudFoundrySpace space in spaces)
                {
                    var newSpace = new SpaceViewModel(space, Services);
                    newSpacesList.Add(newSpace);
                }
            }
            else
            {
                _dialogService.DisplayErrorDialog(_getSpacesFailureMsg, spacesResponse.Explanation);
            }

            return newSpacesList;
        }

        protected internal override async Task LoadChildren()
        {
            var spacesResponse = await CloudFoundryService.GetSpacesForOrgAsync(Org);

            if (spacesResponse.Succeeded)
            {
                if (spacesResponse.Content.Count == 0)
                {
                    var noChildrenList = new ObservableCollection<TreeViewItemViewModel>
                    {
                        EmptyPlaceholder,
                    };

                    Children = noChildrenList;
                    HasEmptyPlaceholder = true;
                }
                else
                {
                    var updatedSpacesList = new ObservableCollection<TreeViewItemViewModel>();
                    foreach (CloudFoundrySpace space in spacesResponse.Content)
                    {
                        var newSpace = new SpaceViewModel(space, Services);
                        updatedSpacesList.Add(newSpace);
                    }

                    Children = updatedSpacesList;
                    HasEmptyPlaceholder = false;
                }

                IsLoading = false;
            }
            else
            {
                IsLoading = false;

                _dialogService.DisplayErrorDialog(_getSpacesFailureMsg, spacesResponse.Explanation);

                IsExpanded = false;
            }
        }
    }
}
