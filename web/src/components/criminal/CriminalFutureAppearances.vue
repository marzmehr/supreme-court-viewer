<template>

    <b-card bg-variant="white" no-body>
        <div>
            <h3 class="mx-4 font-weight-normal" v-if="!showSections['Future Appearances']"> Next Three Future Appearances</h3>
            <hr class="mx-3 bg-light" style="height: 5px;"/> 
        </div>

        <b-card v-if="!isDataReady && isMounted" no-body>
            <span class="text-muted ml-4 mb-5"> No future appearances. </span>
        </b-card>

        <b-card bg-variant="light" v-if= "!isMounted && !isDataReady" >
            <b-overlay :show= "true"> 
                <b-card  style="min-height: 100px;"/>                   
                <template v-slot:overlay>               
                <div> 
                        <loading-spinner/> 
                        <p id="loading-label">Loading ...</p>
                </div>                
                </template> 
            </b-overlay> 
        </b-card>

        <b-card bg-variant="white" v-if="isDataReady" no-body class="mx-3 mb-5" style="overflow:auto">           
            <b-table
            :items="SortedFutureAppearances"
            :fields="fields"
            :sort-by.sync="sortBy"
            :sort-desc.sync="sortDesc"
            :no-sort-reset="true"
            sort-icon-left
            borderless
            @sort-changed="sortChanged"
            small
            responsive="sm"
            >   
                <template v-for="(field,index) in fields" v-slot:[`head(${field.key})`]="data">
                    <b v-bind:key="index" :class="field.headerStyle" > {{ data.label }}</b>
                </template>

                <template  v-slot:cell()="data">
                    <b-badge                        
                        style="font-weight: normal; font-size: 16px; padding-top:12px" 
                        variant="white" > 
                            {{data.value}} 
                    </b-badge>
                </template>

                <template v-slot:cell(Date)="data" >
                    <span :class="data.field.cellClass" style="display: inline-flex;"> 
                        <b-button :style="data.field.cellStyle" 
                                  size="sm" 
                                  @click="OpenDetails(data);data.toggleDetails();" 
                                  variant="outline-primary border-white text-info" 
                                  class="mr-2 mt-1">
                            <b-icon-caret-right-fill v-if="!data.item['_showDetails']"></b-icon-caret-right-fill>
                            <b-icon-caret-down-fill v-if="data.item['_showDetails']"></b-icon-caret-down-fill>
                            {{data.item.FormattedDate}}
                        </b-button>
                    </span> 
                </template>
                <template v-slot:row-details>
                    <b-card> 
                        <criminal-appearance-details/>
                    </b-card>
                </template>

                <template  v-slot:cell(Reason)="data">
                    <b-badge
                            variant="secondary"
                            v-b-tooltip.hover.right                            
                            :title="data.item['Reason Description']"
                            :style="data.field.cellStyle"> 
                            {{data.value}}
                    </b-badge>
                </template>

                <template  v-slot:cell(Accused)="data">
                    <b-badge  
                            variant="white" 
                            :style="data.field.cellStyle" 
                            class = "mt-2"> {{data.value}} 
                    </b-badge>
                </template>

                <template  v-slot:cell(Status)="data">
                    <b :class = "data.item['Status Style']" 
                       :style="data.field.cellStyle"> {{data.value}}
                    </b>
                </template>
                
            </b-table>
        </b-card>
      
    </b-card> 

</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-class";
import * as _ from 'underscore';
import CriminalAppearanceDetails from '@components/criminal/CriminalAppearanceDetails.vue';
import "@store/modules/CommonInformation";
import {criminalAppearancesListType, criminalAppearanceInfoType, criminalFileInformationType} from '../../types/criminal';
import {inputNamesType, durationType} from '../../types/common'
const criminalState = namespace("CriminalFileInformation");
const commonState = namespace("CommonInformation");

enum appearanceStatus {UNCF='Unconfirmed', CNCL='Canceled', SCHD='Scheduled' }

@Component({
    components: {
        CriminalAppearanceDetails
    }
})
export default class CriminalFutureAppearances extends Vue {

    @criminalState.State
    public showSections  

    @commonState.State
    public displayName!: string;    

    @commonState.State
    public duration

    @commonState.State
    public time
    
    @commonState.State
    public statusStyle
    
    @criminalState.State
    public criminalFileInformation!: criminalFileInformationType;

    @criminalState.State
    public appearanceInfo!: criminalAppearanceInfoType;

    @criminalState.Action
    public UpdateAppearanceInfo!: (newAppearanceInfo: criminalAppearanceInfoType) => void

    @commonState.Action
    public UpdateDisplayName!: (newInputNames: inputNamesType) => void

    @commonState.Action
    public UpdateDuration!: (duration: durationType) => void

    @commonState.Action
    public UpdateTime!: (time: string) => void
    
    @commonState.Action
    public UpdateStatusStyle!: (statusStyle: string) => void    
  
    futureAppearancesList: criminalAppearancesListType[] = [];
    isMounted = false;
    isDataReady = false;
    futureAppearancesJson;    
    sortBy = 'Date';
    sortDesc = true;    

    fields =  
    [
        {key:'Date',       sortable:true,  tdClass: 'border-top', headerStyle:'text-primary', cellStyle:'transform: translate(0,-7px); font-size:16px', cellClass:'text-info mt-2 d-inline-flex'},
        {key:'Reason',     sortable:true,  tdClass: 'border-top', headerStyle:'text-primary', cellStyle:'margin-top: 10px; font-size: 14px;'},
        {key:'Time',       sortable:false, tdClass: 'border-top', headerStyle:'text'},
        {key:'Duration',   sortable:false, tdClass: 'border-top', headerStyle:'text'},
        {key:'Location',   sortable:true,  tdClass: 'border-top', headerStyle:'text-primary'},
        {key:'Room',       sortable:false, tdClass: 'border-top', headerStyle:'text'},
        {key:'Accused',    sortable:true,  tdClass: 'border-top', headerStyle:'text-primary', cellStyle:'font-size: 16px;'},
        {key:'Status',     sortable:true,  tdClass: 'border-top', headerStyle:'text-primary', cellStyle:'font-weight: normal; font-size: 16px; width:110px'},
    ];
    
    mounted() {
        this.getFutureAppearances();
    }

    public getFutureAppearances(): void {      
    
        const data = this.criminalFileInformation.detailsData;
        this.futureAppearancesJson = data.appearances.apprDetail;              
        this.ExtractFutureAppearancesInfo();
        if(this.futureAppearancesList.length)
        {                    
            this.isDataReady = true;
        }    
        this.isMounted = true;           
    } 
  
    public ExtractFutureAppearancesInfo(): void {
        const currentDate = new Date();

        for (const appIndex in this.futureAppearancesJson) {
            const appInfo = {} as criminalAppearancesListType;
            const jApp = this.futureAppearancesJson[appIndex];

            appInfo["Index"] = appIndex;
            appInfo["Date"] = jApp.appearanceDt.split(' ')[0]
            if(new Date(appInfo["Date"]) < currentDate) continue;            
            appInfo["FormattedDate"] = Vue.filter('beautify-date')(appInfo["Date"]);
            appInfo["Time"] = this.getTime(jApp.appearanceTm.split(' ')[1].substr(0,5));
            appInfo["Reason"] = jApp.appearanceReasonCd;
            appInfo["Reason Description"] = jApp.appearanceReasonDsc? jApp.appearanceReasonDsc: '';
          
            appInfo["Duration"] = this.getDuration(jApp.estimatedTimeHour, jApp.estimatedTimeMin)           
            appInfo["Location"] = jApp.courtLocation;
            appInfo["Room"] =jApp.courtRoomCd

            appInfo["First Name"] = jApp.givenNm ? jApp.givenNm : "";
            appInfo["Last Name"] = jApp.lastNm ? jApp.lastNm : jApp.orgNm;
            appInfo["Accused"] = this.getNameOfParticipant(appInfo["Last Name"], appInfo["First Name"]);  
            appInfo["Status"] = jApp.appearanceStatusCd ? appearanceStatus[jApp.appearanceStatusCd] :''
            appInfo["Status Style"] = this.getStatusStyle(appInfo["Status"]);
            appInfo["Presider"] =  jApp.judgeInitials ? jApp.judgeInitials :''
            appInfo["Judge Full Name"] =  jApp.judgeInitials ? jApp.judgeFullNm : ''

            appInfo["Appearance ID"] = jApp.appearanceId;            
            appInfo["Part ID"] = jApp.partId;
            appInfo["Supplemental Equipment"] = jApp.supplementalEquipmentTxt;
            appInfo["Security Restriction"] = jApp.securityRestrictionTxt;
            appInfo["OutOfTown Judge"] = jApp.outOfTownJudgeTxt;
            appInfo["Prof Seq No"] = jApp.profSeqNo;           
            this.futureAppearancesList.push(appInfo); 
        }
    }

    public getStatusStyle(status)
    {
        this.UpdateStatusStyle(status);
        return this.statusStyle;
    }

    public getNameOfParticipant(lastName, givenName) {
        this.UpdateDisplayName({'lastName': lastName, 'givenName': givenName});
        return this.displayName;        
    }

    public getTime(time)
    {
        this.UpdateTime(time);
        return this.time;      
    }

    public getDuration(hr, min)
    {
        this.UpdateDuration({'hr': hr, 'min': min});
        return this.duration;
    }

    public OpenDetails(data)
    {
        if(!data.detailsShowing)
        {
            this.appearanceInfo.fileNo = this.criminalFileInformation.fileNumber;            
            this.appearanceInfo.courtLevel = this.criminalFileInformation.courtLevel;
            this.appearanceInfo.courtClass = this.criminalFileInformation.courtClass;
            this.appearanceInfo.appearanceId = data.item["Appearance ID"]
            this.appearanceInfo.partId = data.item["Part ID"]
            this.appearanceInfo.supplementalEquipmentTxt = data.item["Supplemental Equipment"]
            this.appearanceInfo.securityRestrictionTxt = data.item["Security Restriction"]
            this.appearanceInfo.outOfTownJudgeTxt = data.item["OutOfTown Judge"]
            this.appearanceInfo.profSeqNo = data.item["Prof Seq No"]
            this.UpdateAppearanceInfo(this.appearanceInfo);
        }        
    }

    public sortChanged() 
    {
        this.SortedFutureAppearances.forEach((item) => {
            this.$set(item, '_showDetails', false)
        })
    }

    get SortedFutureAppearances()
    {           
        if(this.showSections['Future Appearances'])
        {
            return this.futureAppearancesList;
        }
        else
        {
            return _.sortBy(this.futureAppearancesList,"Date").reverse().slice(0, 3);           
        }
    }
}
</script>

<style scoped>
 .card {
        border: white;
    }
</style>