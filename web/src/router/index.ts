import { RouteConfig } from 'vue-router'
import Home from '@components/Home.vue'
import CivilCaseDetails from "@components/civil/CivilCaseDetails.vue";
import CriminalCaseDetails from "@components/criminal/CriminalCaseDetails.vue";
import CivilFileSearchResultsView from "@components/civil/CivilFileSearchResultsView.vue";
import CriminalFileSearchResultsView from "@components/criminal/CriminalFileSearchResultsView.vue";
import CourtList from "@components/courtlist/CourtList.vue";

const routes: Array<RouteConfig> = [
  {
    path: '/',
    name: 'Home',
    component: Home
  },
  {
    path: '/court-list',
    name: 'CourtList',
    component: CourtList, 
    props: true,
    children: [
      {
        path: 'location/:location/room/:room/date/:date',
        name: 'CourtListResult',
        component: CourtList, 
        props: true
      }
    ]
  },
  {
    path: '/civil-file/:fileNumber',
    name: 'CivilCaseDetails',
    component: CivilCaseDetails, 
    props: true
  },
  {
    path: '/criminal-file/:fileNumber',
    name: 'CriminalCaseDetails',
    component: CriminalCaseDetails,
    props: true
  },
  {
    path: '/civil-file-search',
    name: 'CivilFileSearchResultsView',
    component: CivilFileSearchResultsView, 
    props: true
  },
  {
    path: '/criminal-file-search',
    name: 'CriminalFileSearchResultsView',
    component: CriminalFileSearchResultsView,
    props: true
  }
]

export default routes