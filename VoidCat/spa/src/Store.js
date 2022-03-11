import {configureStore} from "@reduxjs/toolkit";
import loginReducer from "./LoginState";
import siteInfoReducer from "./SiteInfoStore";

export default configureStore({
    reducer: {
        login: loginReducer,
        info: siteInfoReducer
    }
});