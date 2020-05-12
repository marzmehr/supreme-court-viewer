import 'core-js/stable'
import 'regenerator-runtime/runtime'
import 'intersection-observer'
import Vue from 'vue';
import VueResource from 'vue-resource'
import VueRouter from 'vue-router'
import BootstrapVue from "bootstrap-vue";
import "./styles/index.scss";
import "bootstrap-vue/dist/bootstrap-vue.css"
import App from './App.vue';
import routes from './router/index'
import store from './store/index'

Vue.use(VueResource);
Vue.use(VueRouter);
Vue.use(BootstrapVue);
Vue.config.productionTip = true;

const router = new VueRouter({
	mode: 'history',
	routes: routes
});

new Vue({
	router,
	store,
	render: h => h(App)	
}).$mount('#app');