import {createSlice} from "@reduxjs/toolkit";

export const SiteInfoState = createSlice({
    name: "SiteInfo",
    initialState: {
        stats: null
    },
    reducers: {
        setStats: (state, action) => {
            state.stats = action.payload;
        },
    }
});

export const {setStats} = SiteInfoState.actions;
export default SiteInfoState.reducer;