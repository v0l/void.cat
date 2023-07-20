export { VoidApi } from "./api"
export { UploadState } from "./upload";
export { StreamEncryption } from "./stream-encryption";

export class ApiError extends Error {
    readonly statusCode: number

    constructor(statusCode: number, msg: string) {
        super(msg);
        this.statusCode = statusCode;
    }
}

export interface LoginSession {
    jwt?: string
    profile?: Profile
    error?: string
}

export interface AdminUserListResult {
    user: AdminProfile
    uploads: number
}

export interface Profile {
    id: string
    avatar?: string
    name?: string
    created: string
    lastLogin: string
    roles: Array<string>
    publicProfile: boolean
    publicUploads: boolean
    needsVerification?: boolean
}

export interface PagedResponse<T> {
    totalResults: number,
    results: Array<T>
}

export interface AdminProfile extends Profile {
    email: string
    storage: string
}

export interface Bandwidth {
    ingress: number
    egress: number
}

export interface BandwidthPoint {
    time: string
    ingress: number
    egress: number
}

export interface SiteInfoResponse {
    count: number
    totalBytes: number
    buildInfo: {
        version: string
        gitHash: string
        buildTime: string
    }
    bandwidth: Bandwidth
    fileStores: Array<string>
    uploadSegmentSize: number
    captchaSiteKey?: string
    oAuthProviders: Array<string>
    timeSeriesMetrics?: Array<BandwidthPoint>
}

export interface PagedRequest {
    pageSize: number
    page: number
}

export interface VoidUploadResult {
    ok: boolean
    file?: VoidFileResponse
    errorMessage?: string
}

export interface VoidFileResponse {
    id: string,
    metadata?: VoidFileMeta
    payment?: Payment
    uploader?: Profile
    bandwidth?: Bandwidth
    virusScan?: VirusScanStatus
}

export interface VoidFileMeta {
    name?: string
    description?: string
    size: number
    uploaded: string
    mimeType: string
    digest?: string
    expires?: string
    url?: string
    editSecret?: string
    encryptionParams?: string
    magnetLink?: string
    storage: string
}

export interface VirusScanStatus {
    isVirus: boolean
    names?: string
}

export interface Payment {
    service: PaymentServices
    required: boolean
    currency: PaymentCurrencies
    amount: number
    strikeHandle?: string
}

export interface SetPaymentConfigRequest {
    editSecret: string
    currency: PaymentCurrencies
    amount: number
    strikeHandle?: string
    required: boolean
}

export interface PaymentOrder {
    id: string
    status: PaymentOrderState
    orderLightning?: PaymentOrderLightning
}

export interface PaymentOrderLightning {
    invoice: string
    expire: string
}

export interface ApiKey {
    id: string
    created: string
    expiry: string
    token: string
}

export enum PaymentCurrencies {
    BTC = 0,
    USD = 1,
    EUR = 2,
    GBP = 3
}

export enum PaymentServices {
    None = 0,
    Strike = 1
}

export enum PaymentOrderState {
    Unpaid = 0,
    Paid = 1,
    Expired = 2
}

export enum PagedSortBy {
    Name = 0,
    Date = 1,
    Size = 2,
    Id = 3
}

export enum PageSortOrder {
    Asc = 0,
    Dsc = 1
}
