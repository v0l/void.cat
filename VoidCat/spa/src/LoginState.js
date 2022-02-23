import {createSlice} from "@reduxjs/toolkit";

const LocalStorageKey = "token";

export const LoginState = createSlice({
    name: "Login",
    initialState: {
        jwt: window.localStorage.getItem(LocalStorageKey)
    },
    reducers: {
        setAuth: (state, action) => {
            state.jwt = action.payload;
            window.localStorage.setItem(LocalStorageKey, state.jwt);
        },
        logout: (state) => {
            state.jwt = null;
            window.localStorage.removeItem(LocalStorageKey);
        }
    }
});

export const {setAuth, logout} = LoginState.actions;
export default LoginState.reducer;