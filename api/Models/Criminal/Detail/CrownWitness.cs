﻿namespace Scv.Api.Models.Criminal.Detail
{
    /// <summary>
    /// Slimmed down version of JCCommon.Clients.FileServices.CriminalWitness.
    /// </summary>
    public class CrownWitness
    {
        public string PartId { get; set; }
        public string FullName => GivenNm != null && LastNm != null
            ? $"{GivenNm?.Trim()} {LastNm?.Trim()}"
            : null;
        public string LastNm { get; set; }
        public string GivenNm { get; set; }
    }
}
