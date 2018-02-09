using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Tridion.Coreservice.PowerPages.ViewModels
{
    public class PublishViewModel
    {
        public DateTime? FromDateModified { get; set; }
        public DateTime? ToDateModified { get; set; }
        
        public string PublishTargetId { get; set; }

        public string PublicationId { get; set; }

        public List<Component> ComponentsList { get; set; }

        public bool IsSuccess { get; set; }
        public string ServerErrorMessage { get; set; }

        public bool IsPublishCompleted { get; set; }


    }
}