import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import { Profile } from "@void-cat/api";

interface LoginStore {
  jwt?: string;
  profile?: Profile;
}

interface SetAuthPayload {
  jwt: string;
  profile?: Profile;
}

const LocalStorageKey = "token";
export const LoginState = createSlice({
  name: "Login",
  initialState: {
    jwt: window.localStorage.getItem(LocalStorageKey) ?? undefined,
    profile: undefined,
  } as LoginStore,
  reducers: {
    setAuth: (state, action: PayloadAction<SetAuthPayload>) => {
      state.jwt = action.payload.jwt;
      state.profile = action.payload.profile;
      window.localStorage.setItem(LocalStorageKey, state.jwt);
    },
    setProfile: (state, action: PayloadAction<Profile>) => {
      state.profile = action.payload;
    },
    logout: (state) => {
      state.jwt = undefined;
      state.profile = undefined;
      window.localStorage.removeItem(LocalStorageKey);
    },
  },
});

export const { setAuth, setProfile, logout } = LoginState.actions;
export default LoginState.reducer;
