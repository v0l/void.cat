import { configureStore } from "@reduxjs/toolkit";
import loginReducer from "./LoginState";
import siteInfoReducer from "./SiteInfoStore";

const store = configureStore({
  reducer: {
    login: loginReducer,
    info: siteInfoReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
export default store;
