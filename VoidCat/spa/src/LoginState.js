import {createSlice} from "@reduxjs/toolkit";

export const LoginState = createSlice({
    name: "Login",
    initialState: {
        jwt: null
    },
    reducers: {
        setAuth: (state, action) => {
            state.jwt = action.payload;
        },
        logout: (state) => {
            state.jwt = null;
        }
    }
});

export const { setAuth, logout } = LoginState.actions;
export default LoginState.reducer;