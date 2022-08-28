import {createSlice} from "@reduxjs/toolkit";

export const SiteInfoState = createSlice({
    name: "SiteInfo",
    initialState: {
        info: null
    },
    reducers: {
        setInfo: (state, action) => {
            state.info = action.payload;
        },
    }
});

export const {setInfo} = SiteInfoState.actions;
export default SiteInfoState.reducer;