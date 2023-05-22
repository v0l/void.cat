import {createSlice, PayloadAction} from "@reduxjs/toolkit";
import {SiteInfoResponse} from "@void-cat/api";

export const SiteInfoState = createSlice({
    name: "SiteInfo",
    initialState: {
        info: null as SiteInfoResponse | null
    },
    reducers: {
        setInfo: (state, action: PayloadAction<SiteInfoResponse>) => {
            state.info = action.payload;
        },
    }
});

export const {setInfo} = SiteInfoState.actions;
export default SiteInfoState.reducer;