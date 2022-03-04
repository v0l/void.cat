import {createSlice} from "@reduxjs/toolkit";

const LocalStorageKey = "token";
export const LoginState = createSlice({
    name: "Login",
    initialState: {
        jwt: window.localStorage.getItem(LocalStorageKey),
        profile: null
    },
    reducers: {
        setAuth: (state, action) => {
            state.jwt = action.payload.jwt;
            state.profile = action.payload.profile;
            window.localStorage.setItem(LocalStorageKey, state.jwt);
        },
        setProfile: (state, action) => {
            state.profile = action.payload;
        },
        logout: (state) => {
            state.jwt = null;
            state.profile = null;
            window.localStorage.removeItem(LocalStorageKey);
        }
    }
});

export const {setAuth, setProfile, logout} = LoginState.actions;
export default LoginState.reducer;