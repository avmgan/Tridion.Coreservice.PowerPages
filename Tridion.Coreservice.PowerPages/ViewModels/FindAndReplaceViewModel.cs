using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Tridion.Coreservice.PowerPages.ViewModels
{
    public class FindAndReplaceViewModel
    {
        [Required]
        public string SearchText { get; set; }

        [Required]
        public string ReplaceText { get; set; }

        [Required]
        public string FolderId { get; set; }

        public bool Matchcase { get; set; }

        public List<Component> ComponentIdsMatchedListForFindAndReplace { get; set; }

        public List<Component> ComponentIdsNotReplaced { get; set; }
        public bool IsSuccess { get; set; }
        public string ServerErrorMessage { get; set; }

        public bool IsReplaceCompleted { get; set; }
    }

    public class Component
    {
        public string Id { get; set; }
        public string Title { get; set; }

    }
}