﻿using JCCommon.Clients.FileServices;
using System.Collections.Generic;

namespace Scv.Api.Models.Civil.AppearanceDetail
{
    public class CivilAppearanceDetail
    {
        public string PhysicalFileId { get; set; }
        public string AgencyId { get; set; }
        public string AppearanceId { get; set; }
        public string CourtRoomCd { get; set; }
        public string FileNumberTxt { get; set; }
        public string AppearanceDt { get; set; }
        /// <summary>
        /// Extended object. 
        /// </summary>
        public ICollection<CivilAppearanceDetailParty> Party { get; set; }
        
        /// <summary>
        /// Extended document object.
        /// </summary>
        public ICollection<CivilAppearanceDocument> Document { get; set; }
        public ICollection<CivilAppearanceMethod> AppearanceMethod { get; set; }
    }
}