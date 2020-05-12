﻿using JCCommon.Clients.FileServices;
using JCCommon.Models;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Helpers.Exceptions;
using Scv.Api.Models;
using System;
using System.Threading.Tasks;
using Scv.Api.Constants;
using Scv.Api.Models.Civil;
using Scv.Api.Models.Criminal;
using Scv.Api.Services;

namespace Scv.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        #region Variables
        private readonly IConfiguration _configuration;
        private readonly ILogger<FilesController> _logger;
        private readonly FilesService _filesService;

        #endregion

        #region Constructor
        public FilesController(IConfiguration configuration, ILogger<FilesController> logger, FilesService filesService)
        {
            _configuration = configuration;
            _logger = logger;
            _filesService = filesService;
        }
        #endregion

        #region Actions

        #region Civil Only
        /// <summary>
        /// Provides facilities for performing a civil file search. 
        /// </summary>
        /// <param name="fcq">FileCivilQuery object with many parameters</param>
        /// <returns>FileSearchResponse</returns>
        [HttpPost]
        [Route("civil/search")]
        public async Task<ActionResult<FileSearchResponse>> FilesCivilSearchAsync(FilesCivilQuery fcq)
        {
            var fileSearchResponse = await _filesService.FilesCivilAsync(fcq);
            return Ok(fileSearchResponse);
        }

        /// <summary>
        /// Gets the details for a given civil file id. 
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns>RedactedCivilFileDetailResponse</returns>
        [HttpGet]
        [Route("civil/{fileId}")]
        public async Task<ActionResult<RedactedCivilFileDetailResponse>> GetCivilFileDetailByFileId(string fileId)
        {
            var civilFileDetailResponse = await _filesService.FilesCivilFileIdAsync(fileId);
            return Ok(civilFileDetailResponse);
        }

        /// <summary>
        /// Gets appearances for a given civil file id.
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="future">Y to show future, N to hide future</param>
        /// <param name="history">Y to show history, N to hide history</param>
        /// <returns>CivilFileAppearancesResponse</returns>
        [HttpGet]
        [Route("civil/{fileId}/appearances")]
        public async Task<ActionResult<CivilFileAppearancesResponse>> GetCivilAppearancesByFileId(string fileId, FutureYN2? future, HistoryYN2? history)
        {
            var criminalFileIdAppearances = await _filesService.FilesCivilFileIdAppearancesAsync( future, history, fileId);
            return Ok(criminalFileIdAppearances);
        }

        /// <summary>
        /// Gets court summary report for a given appearance id.
        /// </summary>
        /// <param name="appearanceId"></param>
        /// <returns>JustinReportResponse</returns>
        [HttpGet]
        [Route("civil/court-summary-report/{appearanceId}/{fileName?}")]
        public async Task<ActionResult<JustinReportResponse>> GetCivilCourtSummaryReport(string appearanceId)
        {
            var justinReportResponse = await _filesService.FilesCivilCourtsummaryreportAsync(appearanceId, JustinReportName.CEISR035);
            return Ok(justinReportResponse);
        }

        /// <summary>
        /// Gets the civil file content.
        /// This should cover the 1st screenshot. This includes document description, which is important for forming the PDF file names. 
        /// </summary>
        /// <param name="agencyId"></param>
        /// <param name="roomCode"></param>
        /// <param name="proceeding">DateTime? converted to yyyy-MM-dd string</param>
        /// <param name="appearanceId"></param>
        /// <param name="physicalFileId"></param>
        /// <returns>CivilFileContent</returns>
        [HttpGet]
        [Route("civil/file-content")]
        public async Task<ActionResult<CivilFileContent>> GetCivilFileContent(string agencyId = null, string roomCode = null, DateTime? proceeding = null, string appearanceId = null, string physicalFileId = "")
        {
            var civilFileContent = await _filesService.FilesCivilFilecontentAsync(agencyId, roomCode, proceeding, appearanceId, physicalFileId);
            return Ok(civilFileContent);
        }
        #endregion

        #region Criminal Only
        /// <summary>
        /// Provides facilities for performing a criminal file search.  
        /// </summary>
        /// <param name="fcq">FileCriminalQuery, composed of parameters for search. </param>
        /// <returns>FileSearchResponse</returns>
        [HttpPost]
        [Route("criminal/search")]
        public async Task<ActionResult<FileSearchResponse>> FilesCriminalSearchAsync(FilesCriminalQuery fcq)
        {
            var fileSearchResponse = await _filesService.FilesCriminalAsync(fcq);
            return Ok(fileSearchResponse);
        }

        /// <summary>
        /// Get details for a given file id.
        /// </summary>
        /// <param name="fileId">Target file id.</param>
        /// <returns>CriminalFileDetailResponse</returns>
        [HttpGet]
        [Route("criminal/{fileId}")]
        public async Task<ActionResult<RedactedCriminalFileDetailResponse>> GetCriminalFileDetailByFileId(string fileId)
        {
            var redactedCriminalFileDetailResponse = await _filesService.FilesCriminalFileIdAsync(fileId);
            return Ok(redactedCriminalFileDetailResponse);
        }

        /// <summary>
        /// Gets appearances for a given criminal file id.
        /// </summary>
        /// <param name="fileId">Target file id.</param>
        /// <param name="future">Y to show future, N to hide future.</param>
        /// <param name="history">Y to show history, N to hide history.</param>
        /// <returns></returns>
        [HttpGet]
        [Route("criminal/{fileId}/appearances")]
        public async Task<ActionResult<CriminalFileAppearancesResponse>> GetCriminalAppearancesByFileId(string fileId, FutureYN? future = null, HistoryYN? history = null)
        {
            var criminalFileIdAppearances = await _filesService.FilesCriminalFileIdAppearancesAsync(fileId, future, history);
            return Ok(criminalFileIdAppearances);
        }

        /// <summary>
        /// Gets the criminal file content.
        /// This should cover some of the 3rd screenshot.
        /// </summary>
        /// <param name="agencyId"></param>
        /// <param name="roomCode"></param>
        /// <param name="proceeding">The proceeding date in the format YYYY-MM-dd</param>
        /// <param name="appearanceId"></param>
        /// <param name="justinNumber"></param>
        /// <returns>CriminalFileContent</returns>
        [HttpGet]
        [Route("criminal/file-content")]
        public async Task<ActionResult<CriminalFileContent>> GetCriminalFileContent(string agencyId = null, string roomCode = null, DateTime? proceeding = null, string appearanceId = null, string justinNumber = null)
        {
            var criminalFileContent = await _filesService.FilesCriminalFilecontentAsync(agencyId, roomCode, proceeding, appearanceId, justinNumber);
;            return Ok(criminalFileContent);
        }

        /// <summary>
        /// Gets records of proceedings.
        /// </summary>
        /// <param name="partId">The participant id associated to the Record Of Proceedings.</param>
        /// <param name="profSequenceNumber"></param>
        /// <param name="courtLevelCode">The associated court level code.</param>
        /// <param name="courtClassCode">The associated court class code.</param>
        /// <returns></returns>
        [HttpGet]
        [Route("criminal/record-of-proceedings/{partId}/{fileName?}")]
        public async Task<ActionResult<RopResponse>> GetRecordsOfProceeding(string partId, string profSequenceNumber, CourtLevelCd courtLevelCode, CourtClassCd courtClassCode)
        {
            var recordsOfProceeding = await _filesService.FilesRecordOfProceedingsAsync(partId, profSequenceNumber, courtLevelCode, courtClassCode);
            return Ok(recordsOfProceeding);
        }
        #endregion

        /// <summary>
        /// Gets a court list.
        /// </summary>
        /// <param name="agencyId">Agency Identifier Code (Location Code); for example 4801 (Kelona).</param>
        /// <param name="roomCode">The room code; for example </param>
        /// <param name="proceeding">The proceeding date in the format YYYY-MM-dd</param>
        /// <param name="divisionCode">The division code; CR, or CV.</param>
        /// <param name="fileNumber">The full file number; for example 1500-3</param>
        /// <returns>CourtList</returns>
        [HttpGet]
        [Route("court-list")]
        public async Task<ActionResult<CourtList>> GetCourtList(string agencyId, string roomCode, DateTime? proceeding, string divisionCode, string fileNumber)
        {
            var courtList = await _filesService.FilesCourtlistAsync(agencyId, roomCode, proceeding, divisionCode,
                fileNumber);
            return Ok(courtList);
        }

        /// <summary>
        /// Gets a document.
        /// Should cover 2nd and 4th screenshots? 
        /// </summary>
        /// <param name="documentId"></param>
        /// <param name="isCriminal">True if Criminal, False if Civil</param>
        /// <returns>DocumentResponse</returns>
        [HttpGet]
        [Route("document/{documentId}/{fileName?}")]
        public async Task<ActionResult<DocumentResponse>> GetDocument(string documentId, bool isCriminal = false)
        {
            var documentResponse = await _filesService.FilesDocumentAsync(documentId, isCriminal);
            return Ok(documentResponse);
        }
        #endregion

    }
}