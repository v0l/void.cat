import {createSlice} from "@reduxjs/toolkit";

const LocalStorageKey = "token";
const LocalStorageProfileKey = "profile";

export const LoginState = createSlice({
    name: "Login",
    initialState: {
        jwt: window.localStorage.getItem(LocalStorageKey),
        profile: JSON.parse(window.localStorage.getItem(LocalStorageProfileKey))
    },
    reducers: {
        setAuth: (state, action) => {
            state.jwt = action.payload.jwt;
            state.profile = action.payload.profile;
            window.localStorage.setItem(LocalStorageKey, state.jwt);
            window.localStorage.setItem(LocalStorageProfileKey, JSON.stringify(state.profile));
        },
        logout: (state) => {
            state.jwt = null;
            window.localStorage.removeItem(LocalStorageKey);
            window.localStorage.removeItem(LocalStorageProfileKey);
        }
    }
});

export const {setAuth, logout} = LoginState.actions;
export default LoginState.reducer;