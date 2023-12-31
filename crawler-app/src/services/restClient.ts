import axios from "axios"
const axiosRetry = require("axios-retry")

axiosRetry(axios, {retries: 2, retryDelay: axiosRetry.exponentialDelay})
axios.defaults.headers.get["Pragma"] = "no-cache"
axios.defaults.headers.get["Cache-Control"] = "no-cache, no-store"


export interface Response<T = any>  {
    data: T;
    status: number;
    statusText: string;
    headers: any;
    request?: any;
}

export interface RestClientError<T = any> extends Error {
    code?: string;
    request?: any;
    response?: Response<T>;
    isAxiosError: boolean;
    toJSON: () => object;
}

export const getClean =  async <T = any, R = Response<T>>(url: string, token?: string ):Promise<any>  => {
    return await axios
        .get<T, R>(url, setAuthorization(token))
        .then((response: any) => Promise.resolve(response))
        .catch((error: any) => Promise.reject(error));
}


export const get =  async <T = any, R = Response<T>>(url: string, token?: string):Promise<any>  => {
    return await axios
        .get<T, R>(url, setAuthorization(token))
        .then((response: any) => Promise.resolve(response))
        .catch((error: any) => Promise.reject(error));
}

export const del =  async <T = any, R = Response<T>>(url: string, token?: string):Promise<any>  => {
    return await axios
        .delete<T, R>(url, setAuthorization(token))
        .then((response: any) => Promise.resolve(response))
        .catch((error: any) => Promise.reject(error));
}

export const post = async <T = any, R = Response<T>>(url: string, data?: any, token?: string): Promise<R> => {
    return await axios
        .post<T, R>(url, data, setAuthorization(token))
        .then((response: any) => Promise.resolve(response))
        .catch((error: any) => Promise.reject(error));
}

export const put = async <T = any, R = Response<T>>(url: string, data?: any, token?: string): Promise<R> => {
    return await axios
        .put<T, R>(url, data, setAuthorization(token))
        .then((response: any) => Promise.resolve(response))
        .catch((error: any) => Promise.reject(error));;
}


const setAuthorization = (token: string | undefined) => {
    if(token) {
        return {
            headers: {
                authorization: `Bearer ${token}`,
            }
        }
    }

    return {
        headers: {
            authorization: ``,
        }
    }
}

