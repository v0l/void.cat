import {configureStore} from "@reduxjs/toolkit";
import loginReducer from "./LoginState";

export default configureStore({
    reducer: {
        login: loginReducer
    }
});