﻿using JCCommon.Clients.FileServices;
using JCCommon.Clients.LocationServices;
using JCCommon.Models;
using LazyCache;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Services;
using Scv.Api.Services.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Scv.Api.Helpers.Exceptions;
using tests.api.Helpers;
using Xunit;
using System.Text;
using System.Threading.Tasks;
using JCCommon.Clients.LookupCodeServices;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Scv.Api.Helpers;
using Scv.Api.Models.archive;

namespace tests.api.Controllers
{
    /// <summary>
    /// These tests, ensure Api.FilesController and JC-Client-Interface.FileServicesClient work correctly.
    /// Credit to DARS for most of these tests.
    /// Note: these tests are intended for the development environment.
    /// </summary>
    public class FilesControllerTests
    {
        #region Variables

        private readonly FilesController _controller;
        private readonly FileServicesClient _fileServicesClient;
        #endregion Variables

        #region Constructor

        public FilesControllerTests()
        {
            var fileServices = new EnvironmentBuilder("FileServicesClient:Username", "FileServicesClient:Password", "FileServicesClient:Url");
            var lookupServices = new EnvironmentBuilder("LookupServicesClient:Username", "LookupServicesClient:Password", "LookupServicesClient:Url");
            var locationServices = new EnvironmentBuilder("LocationServicesClient:Username", "LocationServicesClient:Password", "LocationServicesClient:Url");
            var lookupServiceClient = new LookupCodeServicesClient(lookupServices.HttpClient);
            var locationServiceClient = new LocationServicesClient(locationServices.HttpClient);
            var fileServicesClient = new FileServicesClient(fileServices.HttpClient);
            _fileServicesClient = fileServicesClient;
            var lookupService = new LookupService(lookupServices.Configuration, lookupServiceClient, new CachingService());
            var locationService = new LocationService(locationServices.Configuration, locationServiceClient, new CachingService());

            var claims = new[] {
                new Claim(CustomClaimTypes.JcParticipantId,  fileServices.Configuration.GetNonEmptyValue("Request:PartId")),
                new Claim(CustomClaimTypes.JcAgencyCode, fileServices.Configuration.GetNonEmptyValue("Request:AgencyIdentifierId")),
            };
            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            var filesService = new FilesService(fileServices.Configuration, fileServicesClient, new Mapper(), lookupService, locationService, new CachingService(), principal);
            _controller = new FilesController(fileServices.Configuration, fileServices.LogFactory.CreateLogger<FilesController>(), filesService);
            _controller.ControllerContext = HttpResponseTest.SetupMockControllerContext(fileServices.Configuration);
        }

        #endregion Constructor

        #region Tests

        [Fact]
        public async void Civil_Document_With_Reference_Document()
        {
            var actionResult = await _controller.GetCivilFileDetailByFileId("3822");

            var fileDetailResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);

            var referenceDocuments = fileDetailResponse.ReferenceDocument;

            Assert.NotNull(referenceDocuments);
            Assert.Equal(4, referenceDocuments.Count);
            var firstReferenceDocument = referenceDocuments.First();

            var document = await _controller.GetDocument(WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(firstReferenceDocument.ObjectGuid)), "hello.txt", false);
        }

        [Fact]
        public async void Civil_File_With_Reference_Documents()
        {
            var actionResult = await _controller.GetCivilFileDetailByFileId("3822");

            var fileDetailResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);

            var referenceDocuments = fileDetailResponse.ReferenceDocument;

            Assert.NotNull(referenceDocuments);
            Assert.Equal(4,referenceDocuments.Count);
            var firstReferenceDocument = referenceDocuments.First();

            Assert.Equal("2020-12-03 00:00:00.0", firstReferenceDocument.AppearanceDate);
            Assert.Equal("13915", firstReferenceDocument.AppearanceId);
            Assert.Equal("Notice of Application filed January 2 2020", firstReferenceDocument.DescriptionText);
            Assert.Equal("4462", firstReferenceDocument.PartyId);
            Assert.Equal("TEST, One", firstReferenceDocument.PartyName);
        }

        [Fact]
        public async void Civil_File_Document_Filed_By_Name()
        {
            var actionResult = await _controller.GetCivilFileDetailByFileId("3834");

            var fileDetailResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);

            var document = fileDetailResponse.Document.FirstOrDefault(doc => doc.CivilDocumentId == "10672");
            Assert.NotNull(document);
            Assert.Equal(2, document.FiledBy.Count());
            Assert.NotNull(document.FiledBy.FirstOrDefault());
            Assert.Equal("CHAMBERS, Martin", document.FiledBy.FirstOrDefault().FiledByName);
            Assert.Equal("PLA", document.FiledBy.FirstOrDefault().RoleTypeCode);
        }

        [Fact]
        public async void Civil_File_Services_File_Content()
        {
            /* This is the largest civil file on dev. Unfortunately if the WSDL changes for this route, 
             * it will always return back 200, but a null file. It would have been nice if the server 
             * would return 500 etc on errors. */
            var result = await _fileServicesClient.FilesCivilFilecontentAsync(null, null, null, null, "2222", "SCV");
            Assert.NotNull(result);
        }

        [Fact]
        public async void Criminal_File_Details_ByFileNumberText_Three()
        {
            var actionResult = await _controller.GetCriminalFileIdsByAgencyIdCodeAndFileNumberText("83.0001", "98050101");

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal(3, fileSearchResponse.Count);
        }

        [Fact]
        public async void Civil_File_Details_ByFileNumberText_Multiple()
        {
            var actionResult =
                await _controller.GetCivilFileIdsByAgencyIdCodeAndFileNumberText("83.0001", "1");

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal(2, fileSearchResponse.Count);
        }

        [Fact]
        public async void Criminal_File_Details_ByFileNumberText_Multiple()
        {
            var actionResult =
                await _controller.GetCriminalFileIdsByAgencyIdCodeAndFileNumberText("83.0001", "58819");

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal(3, fileSearchResponse.Count);
        }

        [Fact]
        public async void Criminal_File_Details_By_FileNumberText_Empty()
        {
            var failed = false;
            try
            {
                var actionResult =
                    await _controller.GetCriminalFileIdsByAgencyIdCodeAndFileNumberText("83.0001", "500-24747474774");
            }
            catch (NotFoundException)
            {
                failed = true;
            }

            Assert.True(failed);
        }

        [Fact]
        public async void Civil_File_Details_By_FileNumberText_Empty()
        {
            var failed = false;
            try
            {
                var actionResult =
                    await _controller.GetCivilFileIdsByAgencyIdCodeAndFileNumberText("104.0001", "P-24166666666");
            }
            catch (NotFoundException)
            {
                failed = true;
            }
            Assert.True(failed);
        }

        [Fact]
        public async void Criminal_File_Details_By_FileNumberText()
        {
            var actionResult = await _controller.GetCriminalFileIdsByAgencyIdCodeAndFileNumberText("83.0001", "58819-1");

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Contains("3777", fileSearchResponse.First().JustinNo);
        }


        [Fact]
        public async void Civil_File_Details_By_FileNumberText_2()
        {
            var actionResult = await _controller.GetCivilFileIdsByAgencyIdCodeAndFileNumberText("83.0001", "S-1");

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Contains("1619", fileSearchResponse.First().PhysicalFileId);
        }

        [Fact]
        public async void Civil_File_Details_By_FileNumberText()
        {
            var actionResult = await _controller.GetCivilFileIdsByAgencyIdCodeAndFileNumberText("83.0001", "44459");

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Contains("3059", fileSearchResponse.First().PhysicalFileId);
        }

        
        [Fact]
        public async void Criminal_File_Search_By_LastName()
        {
            var fcq = new FilesCriminalQuery
            {
                SearchMode = SearchMode.PARTNAME,
                FileHomeAgencyId = "83.0001",
                LastName = "Sm"
            };
            var actionResult = await _controller.FilesCriminalSearchAsync(fcq);

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Contains(fileSearchResponse.FileDetail, fd => fd.Participant.Any(p => p.FullNm.Contains("Smith")));
        }

        [Fact]
        public async void Civil_File_Search_By_LastName()
        {
            var fcq = new FilesCivilQuery
            {
                SearchMode = SearchMode2.PARTNAME,
                FileHomeAgencyId = "83.0001",
                LastName = "bad",
                CourtLevel = CourtLevelCd3.P
            };
            var actionResult = await _controller.FilesCivilSearchAsync(fcq);

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("1", fileSearchResponse.RecCount);
            Assert.Equal(1, fileSearchResponse.FileDetail.Count);
            Assert.Contains("2437", fileSearchResponse.FileDetail.First().PhysicalFileId);
            Assert.Contains("BADGUY, Borris", fileSearchResponse.FileDetail.First().Participant.Select(u => u.FullNm));
        }

        [Fact]
        public async void Criminal_File_Search_By_JustinNo()
        {
            var fcq = new FilesCriminalQuery
            {
                SearchMode = SearchMode.JUSTINNO,
                FileHomeAgencyId = "83.00001",
                MdocJustinNoSet = "35674"
            };
            var actionResult = await _controller.FilesCriminalSearchAsync(fcq);

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Contains(fileSearchResponse.FileDetail, fd => fd.MdocJustinNo == "35674");
        }

        [Fact]
        public async void Civil_File_Search_By_PhysicalFileId()
        {
            var fcq = new FilesCivilQuery
            {
                SearchMode = SearchMode2.PHYSID,
                FileHomeAgencyId = "83.0001",
                PhysicalFileIdSet = "2506"
            };
            var actionResult = await _controller.FilesCivilSearchAsync(fcq);

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("1", fileSearchResponse.RecCount);
            Assert.Equal(1, fileSearchResponse.FileDetail.Count);
            Assert.Equal("C-11011", fileSearchResponse.FileDetail.First().FileNumberTxt);
            Assert.Contains("BYSTANDER, Innocent", fileSearchResponse.FileDetail.First().Participant.Select(u => u.FullNm));
        }

        [Fact]
        public async void Criminal_File_Details_by_JustinNo()
        {
            var actionResult = await _controller.GetCriminalFileDetailByFileId("35674");

            var redactedCriminalFileDetailResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("35674", redactedCriminalFileDetailResponse.JustinNo);
            Assert.True(redactedCriminalFileDetailResponse.Participant.Count > 0);
        }

        [Fact]
        public async void Civil_File_Details_by_PhysicalFileId()
        {
            var actionResult = await _controller.GetCivilFileDetailByFileId("40");

            var redactedCivilFileDetailResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("40", redactedCivilFileDetailResponse.PhysicalFileId);
            Assert.Equal("P-241", redactedCivilFileDetailResponse.FileNumberTxt);
            Assert.Contains(redactedCivilFileDetailResponse.Party, f => f.LastNm == "Kings");
            Assert.Contains(redactedCivilFileDetailResponse.Party, f => f.LastNm == "Jones");
        }

        [Fact]
        public async void Criminal_Appearances_by_JustinNo()
        {
            var actionResult = await _controller.GetCriminalFileDetailByFileId("35674");

            var criminalFileAppearancesResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Contains(criminalFileAppearancesResponse.Appearances.ApprDetail,
                f => f.LastNm == "Young" && f.GivenNm == "Johnny");
        }

        [Fact]
        public async void Civil_Appearances_by_PhysicalFileId()
        {
            var actionResult = await _controller.GetCivilFileDetailByFileId("2506");

            var civilFile = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("0", civilFile.Appearances.FutureRecCount);
            Assert.Equal("20", civilFile.Appearances.HistoryRecCount);
        }

        [Fact]
        public async void Criminal_File_Content()
        {
            var actionResult = await _controller.GetCriminalFileContent("4801", "101", DateTime.Parse("2016-04-04"), "44150.0734");

            var criminalFileContent = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("4801", criminalFileContent.CourtLocaCd);
            Assert.Equal("101", criminalFileContent.CourtRoomCd);
            Assert.Equal("2016-04-04", criminalFileContent.CourtProceedingDate);
        }

        [Fact]
        public async void Civil_File_Content_By_AgencyId_Room_Proceeding_Appearance()
        {
            var actionResult = await _controller.GetCivilFileContent("4801", "101", DateTime.Parse("2016-04-04"), "984");

            var civilFileContent = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("4801", civilFileContent.CourtLocaCd);
            Assert.Equal("101", civilFileContent.CourtRoomCd);
            Assert.Equal("2016-04-04", civilFileContent.CourtProceedingDate);
            Assert.Equal(1, civilFileContent.CivilFile.Count);
            Assert.Equal("2506", civilFileContent.CivilFile.First().PhysicalFileID);
        }

        [Fact]
        public async void Criminal_File_Search_By_FileNo_Provincial()
        {
            var fcq = new FilesCriminalQuery
            {
                SearchMode = SearchMode.FILENO,
                FileHomeAgencyId = "83.0001",
                FileNumberTxt = "98050101",
                CourtLevel = CourtLevelCd2.P
            };
            var actionResult = await _controller.FilesCriminalSearchAsync(fcq);

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("2", fileSearchResponse.RecCount);
        }

        [Fact]
        public async void Criminal_File_Search_By_FileNo_Supreme()
        {
            var fcq = new FilesCriminalQuery
            {
                SearchMode = SearchMode.FILENO,
                FileHomeAgencyId = "83.0001",
                FileNumberTxt = "98050101",
                CourtLevel = CourtLevelCd2.S
            };
            var actionResult = await _controller.FilesCriminalSearchAsync(fcq);

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("1", fileSearchResponse.RecCount);
        }

        [Fact]
        public async void Criminal_File_Search_By_FileNo_ProvincialAndSupreme()
        {
            var fcq = new FilesCriminalQuery
            {
                SearchMode = SearchMode.FILENO,
                FileHomeAgencyId = "83.0001",
                FileNumberTxt = "98050101",
            };
            var actionResult = await _controller.FilesCriminalSearchAsync(fcq);

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("3", fileSearchResponse.RecCount);
        }

        [Fact]
        public async void Civil_File_Search_By_FileNo_Provincial()
        {
            var fcq = new FilesCivilQuery
            {
                SearchMode = SearchMode2.FILENO,
                FileHomeAgencyId = "83.0001",
                FileNumber = "11011",
                CourtLevel = CourtLevelCd3.P
            };
            var actionResult = await _controller.FilesCivilSearchAsync(fcq);

            var fileSearchResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("1", fileSearchResponse.RecCount);
            Assert.Equal(1, fileSearchResponse.FileDetail.Count);
            Assert.Equal("2506", fileSearchResponse.FileDetail.First().PhysicalFileId);
            Assert.Contains("BYSTANDER, Innocent", fileSearchResponse.FileDetail.First().Participant.Select(u => u.FullNm));
        }

        [Fact]
        public async void Civil_Court_Summary_Report()
        {
            var actionResult = await _controller.GetCivilCourtSummaryReport("984", "test123.pdf");

            var fileContentResult = actionResult as FileContentResult;
            Assert.NotNull(fileContentResult);
            Assert.True(fileContentResult.FileContents.Length > 3200);
        }

        [Fact]
        public async void Criminal_Record_Of_Proceeding()
        {
            var actionResult = await _controller.GetRecordsOfProceeding("12971.0026", "ropTest.pdf", "24", CourtLevelCd.P, CourtClassCd.A);

            var fileContentResult = actionResult as FileContentResult;
            Assert.NotNull(fileContentResult);
            Assert.True(fileContentResult.FileContents.Length > 79000);
        }

        [Fact]
        public async void Civil_File_Content_By_FileId()
        {
            var actionResult = await _controller.GetCivilFileContent(physicalFileId: "2506");

            var civilFileContent = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Null(civilFileContent.CourtLocaCd);
            Assert.Null(civilFileContent.CourtRoomCd);
            Assert.Equal("", civilFileContent.CourtProceedingDate);
            Assert.Equal(1, civilFileContent.CivilFile.Count);
            Assert.Equal("2506", civilFileContent.CivilFile.First().PhysicalFileID);
        }

        [Fact]
        public async void Document_Civil()
        {
            var actionResult = await _controller.GetDocument(WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("10010")), "test.pdf");

            var fileContentResult = actionResult as FileContentResult;
            Assert.NotNull(fileContentResult);
            Assert.Equal(14625, fileContentResult.FileContents.Length);
        }

        [Fact]
        public async void Document_Criminal()
        {
            var actionResult = await _controller.GetDocument(WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("40")), "test.pdf", true);

            var fileContentResult = actionResult as FileContentResult;
            Assert.NotNull(fileContentResult);
            Assert.True(fileContentResult.FileContents.Length > 100000);
        }

        [Fact]
        public async void Criminal_File_Content_By_JustinNumber()
        {
            var actionResult = await _controller.GetCriminalFileContent(justinNumber: "3179.0000");

            var criminalFileContent = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("", criminalFileContent.CourtLocaCd);
            Assert.Equal("", criminalFileContent.CourtRoomCd);
            Assert.Equal("", criminalFileContent.CourtProceedingDate);
            Assert.Equal(1, criminalFileContent.AccusedFile.Count);
            Assert.Equal("3179", criminalFileContent.AccusedFile.First().MdocJustinNo);
        }

        #endregion Tests

        [Fact]
        public async void Criminal_File_Detail_Document_By_JustinNumber()
        {
            var actionResult = await _controller.GetCriminalFileDetailByFileId(fileId: "35840");

            var criminalFileDocuments = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal(4, criminalFileDocuments.Participant.First().Document.Count);
            Assert.Contains(criminalFileDocuments.Participant.First().Document,
                doc => doc.DocmFormDsc == "Summons Criminal Code (With a very long name so I can test Cannabis names)");
            Assert.Contains(criminalFileDocuments.Participant.First().Document, doc => doc.PartId == "61145.0002");
        }

        [Fact]
        public async void Civil_Appearance_Details()
        {
            //Has party data.
            var actionResult = await _controller.GetCivilAppearanceDetails("2506", "11034");

            var civilAppearanceDetail = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal(3, civilAppearanceDetail.Party.Count);
            Assert.Contains(civilAppearanceDetail.Party, p => p.LastNm == "BYSTANDER");

            //Has appearanceMethod data.
            actionResult = await _controller.GetCivilAppearanceDetails("3499", "13410");

            civilAppearanceDetail = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal(1, civilAppearanceDetail.AppearanceMethod.Count);
            Assert.Equal("IP", civilAppearanceDetail.AppearanceMethod.First().AppearanceMethodCd);
        }

        [Fact]
        public async void Criminal_Appearance_Details()
        {
            var actionResult = await _controller.GetCriminalAppearanceDetails("2934", "36548.0734", "19498.0042");

            var criminalAppearanceDetail = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal(1, criminalAppearanceDetail.Charges.Count);
            Assert.Equal("2934", criminalAppearanceDetail.JustinNo);
            Assert.Contains(criminalAppearanceDetail.Charges, p => p.AppearanceReasonDsc == "First Appearance");
            Assert.Contains(criminalAppearanceDetail.Charges, p => p.StatuteDsc == "offer bribe to justice/pol comm/peac off");
            Assert.Contains(criminalAppearanceDetail.Charges, p => p.StatuteSectionDsc == "CCC - 120(b)");
            Assert.Equal(1, criminalAppearanceDetail.AppearanceMethods.Count);
            Assert.Equal("TC", criminalAppearanceDetail.AppearanceMethods.First().AppearanceMethodCd);
        }

        [Fact]
        public async void Criminal_Appearance_Details_Accused_Prosecutor_Adjudicator()
        {
            var actionResult = await _controller.GetCriminalAppearanceDetails("2800", "34595.0734", "13816.0026");

            var criminalAppearanceDetail = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);

            Assert.Equal("2800", criminalAppearanceDetail.JustinNo);
            Assert.Contains(criminalAppearanceDetail.Charges, p => p.AppearanceReasonDsc == "First Appearance");
            Assert.Equal("JONES BARB", criminalAppearanceDetail.JustinCounsel.FullName);
            Assert.Equal("Woody Allan", criminalAppearanceDetail.Accused.FullName);
            Assert.Equal("Telephone Conference", criminalAppearanceDetail.Accused.AttendanceMethodDesc);
            Assert.Equal("Willie Smith", criminalAppearanceDetail.Prosecutor.FullName);
            Assert.Equal("26139.0045", criminalAppearanceDetail.Prosecutor.PartId);
            Assert.Equal("R Butler Mon Ami", criminalAppearanceDetail.Adjudicator.FullName);
        }

        [Fact]
        public async void Criminal_Appearance_Details_No_Prosecutor_Adjudicator()
        {
            var actionResult = await _controller.GetCriminalAppearanceDetails("2934", "36548.0734", "19498.0042");

            var criminalAppearanceDetail = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);

            Assert.Equal("2934", criminalAppearanceDetail.JustinNo);
            Assert.Equal("Telephone Conference", criminalAppearanceDetail.Accused.AttendanceMethodDesc);
            Assert.Equal("TC", criminalAppearanceDetail.Accused.AttendanceMethodCd);
            Assert.Equal("Telephone Conference", criminalAppearanceDetail.AppearanceMethods.First().AppearanceMethodDesc);
            Assert.Equal("TC", criminalAppearanceDetail.AppearanceMethods.First().AppearanceMethodCd);
            Assert.Equal("79221.0734", criminalAppearanceDetail.JustinCounsel.CounselPartId);
            Assert.Null(criminalAppearanceDetail.Prosecutor);
            Assert.Null(criminalAppearanceDetail.Adjudicator);
        }

        [Fact]
        public async void Criminal_Appearance_Details_Prosecutor_Adjudicator_Accused()
        {
            var actionResult = await _controller.GetCriminalAppearanceDetails("1009", "1169.0026", "14188.0026");

            var criminalAppearanceDetail = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);

            Assert.Equal("1009", criminalAppearanceDetail.JustinNo);
            Assert.Equal("Stephen Frank Lewis", criminalAppearanceDetail.Accused.FullName);
            Assert.Equal("P", criminalAppearanceDetail.Accused.PartyAppearanceMethod);
            Assert.Equal("Present", criminalAppearanceDetail.Accused.PartyAppearanceMethodDesc);  //Doesn't seem to have any appearance methods
            Assert.Equal("Michael Jordan", criminalAppearanceDetail.Adjudicator.FullName);
            Assert.Equal("14007.0026", criminalAppearanceDetail.Adjudicator.PartId);
            Assert.Equal("Brad Bow Baggins Stez", criminalAppearanceDetail.Prosecutor.FullName);
            Assert.Equal("19.0001", criminalAppearanceDetail.Prosecutor.PartId);
        }

        [Fact]
        public async void Criminal_Appearance_Details_AttendanceMethod_PartyAppearanceMethod_AppearanceMethod()
        {
            var actionResult = await _controller.GetCriminalAppearanceDetails("3058", "30503.0734", "19621.0042");

            var criminalAppearanceDetail = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("VC", criminalAppearanceDetail.JustinCounsel.AppearanceMethodCd);
            Assert.Equal("VC", criminalAppearanceDetail.JustinCounsel.AttendanceMethodCd);
            Assert.Equal("CV", criminalAppearanceDetail.JustinCounsel.PartyAppearanceMethod);
            Assert.Equal("TC", criminalAppearanceDetail.Prosecutor.AppearanceMethodCd);
            Assert.Equal("TC", criminalAppearanceDetail.Prosecutor.AttendanceMethodCd);
            Assert.Equal("T", criminalAppearanceDetail.Prosecutor.PartyAppearanceMethod);
            Assert.Equal("VC", criminalAppearanceDetail.Accused.AppearanceMethodCd);
            Assert.Equal("VC", criminalAppearanceDetail.Accused.AttendanceMethodCd);
            Assert.Equal("PV", criminalAppearanceDetail.Accused.PartyAppearanceMethod);
        }

        [Fact]
        public async void Civil_Appearance_Details_Adjudicator()
        {
            var actionResult = await _controller.GetCivilAppearanceDetails("2255", "13403");

            var civilAppearanceDetail = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("This is the comment i made to Guy Landry ", civilAppearanceDetail.AdjudicatorComment);
            Assert.Equal("Butler Mon Ami, R", civilAppearanceDetail.Adjudicator.FullName);
            Assert.Equal("In Person", civilAppearanceDetail.Adjudicator.AdjudicatorAppearanceMethodDesc);
            Assert.Equal("IP", civilAppearanceDetail.Adjudicator.AdjudicatorAppearanceMethod);
        }

        [Fact]
        public async void Civil_Appearance_Details_Party_AppearanceMethods()
        {
            //This test sees if our AppearanceMethod data is tied into the party objects.
            var actionResult = await _controller.GetCivilAppearanceDetails("2222", "12047");

            var civilAppearanceDetail = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);

            //Here we have AppearanceMethods, for adjudicator.  Note no data for name.
            Assert.Equal("VC", civilAppearanceDetail.Adjudicator?.AppearanceMethodCd);
            Assert.Equal("Video Conference", civilAppearanceDetail.Adjudicator.AppearanceMethodDesc);
            //Also data for applicant.
            Assert.True(civilAppearanceDetail.Party.Where(p => p.PartyRole.Any(pr => pr.RoleTypeCd == "APP"))
                .All(p => p.AppearanceMethodCd == "VC"));
        }

        [Fact]
        public async void Civil_Appearance_Details_Party_CourtList_AttendanceMethod()
        {
            var actionResult = await _controller.GetCivilAppearanceDetails("100", "19");

            var civilAppearanceDetail = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Contains(civilAppearanceDetail.Party, p => p.PartyId == "11" && p.AttendanceMethodCd == "TC");
        }

        [Fact]
        public async void Civil_Appearance_Details_Party_CourtList_LegalRepresentative()
        {
            var actionResult = await _controller.GetCivilAppearanceDetails("1984", "8344");

            var civilAppearanceDetail = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            var legalRepresentativeParty = civilAppearanceDetail.Party.FirstOrDefault(p => p.PartyId == "896");
            Assert.NotNull(legalRepresentativeParty);
            Assert.NotEmpty(legalRepresentativeParty.LegalRepresentative);
            Assert.True(legalRepresentativeParty.LegalRepresentative.First().LegalRepTypeDsc == "Litigation Guardian");
            Assert.True(legalRepresentativeParty.LegalRepresentative.First().LegalRepFullName == "SMITH AND BARNEY ASSOCIATES");
        }

        [Fact]
        public async void Civil_Appearance_Details_Party_CourtList_Counsel()
        {
            var actionResult = await _controller.GetCivilAppearanceDetails("2436", "8430");

            var civilAppearanceDetail = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            var counselParty = civilAppearanceDetail.Party.FirstOrDefault(p => p.PartyId == "1928");
            Assert.NotNull(counselParty);
            Assert.NotEmpty(counselParty.Counsel);
            Assert.Equal("119", counselParty.Counsel.First().CounselId);
            Assert.Equal("PETER, John", counselParty.Counsel.First().CounselFullName);
            Assert.Equal("(547)123-1233", counselParty.Counsel.First().PhoneNumber);
        }

        [Fact]
        public async void Civil_Appearance_Details_Party_CourtList_Representative()
        {
            var actionResult = await _controller.GetCivilAppearanceDetails("2307", "9403");

            var civilAppearanceDetail = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            var representativeParty = civilAppearanceDetail.Party.FirstOrDefault(p => p.PartyId == "1112");
            Assert.NotNull(representativeParty);
            Assert.NotEmpty(representativeParty.Representative);
            Assert.Equal("bla", representativeParty.Representative.First().RepFullName);
        }

        [Fact]
        public async void Civil_Appearance_Details_Party_PreviousAppearance_PartyAppearanceMethod()
        {
            var actionResult = await _controller.GetCivilAppearanceDetails("100", "19");

            var civilAppearanceDetail = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            var party = civilAppearanceDetail.Party.FirstOrDefault(p => p.PartyId == "21");
            Assert.NotNull(party);
            Assert.Equal("P", party.PartyAppearanceMethod);
            Assert.Equal("Present", party.PartyAppearanceMethodDesc); //Doesn't seem to have any appearance methods
        }

        [Fact]
        public async Task Get_Archive()
        {
            var archiveRequest = new ArchiveRequest
            {
                ZipName = "Hello.zip",
                CsrRequests = new List<CsrRequest>
                {
                    new CsrRequest
                    {
                        AppearanceId = "984",
                        PdfFileName = "test123.pdf"
                    }
                },
                DocumentRequests = new List<DocumentRequest>
                {
                    new DocumentRequest
                    {
                        Base64UrlEncodedDocumentId = "MzUyMi4wNzM0",
                        IsCriminal = true,
                        PdfFileName = "55"
                    },
                    new DocumentRequest
                    {
                        Base64UrlEncodedDocumentId = "R2VXekdJRG1HTVV1LiNDR01LdjkuaWBFfGFoUCdQXl56PnFLKltzZlV3PU0iOlp4ZilmIydBSSg5Mk8yYWRFJ1ZHQTlfNy4zNjgyNDYwMDAuMDc0NzExLjI0NTkxODQuLiNDNA",
                        IsCriminal = false,
                        PdfFileName = "4646363"
                    }
                },
                RopRequests = new List<RopRequest>
                {
                    new RopRequest
                    {
                        CourtLevelCode = CourtLevelCd.P,
                        CourtClassCode = CourtClassCd.A,
                        PartId = "12971.0026",
                        PdfFileName = "ropTest.pdf",
                        ProfSequenceNumber = "24"
                    }
                }
            };

            var actionResult = await _controller.GetArchive(archiveRequest) as FileContentResult;
            Assert.NotNull(actionResult);
            Assert.Equal("Hello.zip", actionResult.FileDownloadName);
            Assert.True(actionResult.FileContents.Length > 0);
        }

        [Fact(Skip = "Adhoc Test")]
        public async void Criminal_Appearance_Details_CacheTest()
        {
            //This fetches the FileDetail plus the appearances. So these should be cached after this call.
            var actionResult = await _controller.GetCriminalFileDetailByFileId("2934");
            var fileDetail = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);

            //Now call criminalAppearanceDetails.
            var actionResult2 = await _controller.GetCriminalAppearanceDetails("2934", "36548.0734", "19498.0042");

            var criminalAppearanceDetail = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult2);
            Assert.Equal(1, criminalAppearanceDetail.Charges.Count);
            Assert.Equal("2934", criminalAppearanceDetail.JustinNo);
            Assert.Contains(criminalAppearanceDetail.Charges, p => p.AppearanceReasonDsc == "First Appearance");
            Assert.Contains(criminalAppearanceDetail.Charges, p => p.StatuteDsc == "offer bribe to justice/pol comm/peac off");
            Assert.Contains(criminalAppearanceDetail.Charges, p => p.StatuteSectionDsc == "CCC - 120(b)");
            Assert.Equal(1, criminalAppearanceDetail.AppearanceMethods.Count);
            Assert.Equal("TC", criminalAppearanceDetail.AppearanceMethods.First().AppearanceMethodCd);
        }

        #region Helpers

        private void SetupMocks()
        {
            var headerDictionary = new HeaderDictionary();
            var response = new Mock<HttpResponse>();
            response.SetupGet(r => r.Headers).Returns(headerDictionary);

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(a => a.Response).Returns(response.Object);

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext.Object
            };
        }

        #endregion Helpers
    }
}