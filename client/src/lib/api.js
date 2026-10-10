// lib/api.js
import { resolve } from '$app/paths';
import { goto } from '$app/navigation';
import axios from 'axios';
axios.defaults.withCredentials = true;
import { isLoading, userInfo } from '#lib/store.js';
import { apiErrMsg, crud } from '#lib/const.js';

const rootApi = import.meta.env.VITE_API_URL;
const isSelfHostedClient = rootApi.indexOf('http') < 0;
const apiUri = isSelfHostedClient ? `${window.location.origin}/${rootApi}` : `${rootApi}`;
const isMock = apiUri.indexOf('mock') > -1;
const mock = isMock ? '.json' : '';

/** @param {string} cookieName */
const getCsrfToken = (cookieName) => {
  const regex = new RegExp(`(^| )${cookieName}=([^;]+)`);
  const match = document.cookie.match(regex);
  return match ? decodeURIComponent(match[2]) : '';
};

/** @param {import('axios').AxiosRequestConfig} params */
const axiosCallAsync = async (params) => {
  isLoading.set(true);
  try {
    const result = await axios(params);
    return result.data;
  } catch (error) {
    return { error: getErrorMsg(error) };
  } finally {
    isLoading.set(false);
  }
};

export const apiGetUserInfoAsync = async () => {
  const getMsg = {
    method: 'get',
    url: `${apiUri}/auth/userinfo${mock}`
  };
  const result = await axiosCallAsync(getMsg);
  if (result.error) {
    userInfo.set(null);
  } else {
    userInfo.set(result);
  }

  return result;
};

export const apiLogin = () => {
  window.location.assign(isMock ? '/' : apiUri.replace('api', ''));
};

export const apiLogoutAsync = async () => {
  const getMsg = {
    method: 'get',
    url: `${apiUri}/auth/logout${mock}`
  };
  const result = await axiosCallAsync(getMsg);
  window.location.assign(result);
};

/** @param {string} search */
export const apiSearchStuffAsync = async (search) => {
  const getMsg = {
    method: 'get',
    url: `${apiUri}/stuff${mock}?search=${search}`
  };
  return await axiosCallAsync(getMsg);
};

export const apiGetStuffListAsync = async () => {
  const getMsg = {
    method: 'get',
    url: `${apiUri}/stuff${mock}`
  };
  return await axiosCallAsync(getMsg);
};

/** @param {number} page */
export const apiGotoPageAsync = async (page) => {
  const getMsg = {
    method: 'get',
    url: `${apiUri}/stuff${mock}?page=${page}`
  };
  return await axiosCallAsync(getMsg);
};

/** @param {string} id */
export const apiGetStuffByIdAsync = async (id) => {
  const getMsg = {
    method: 'get',
    url: `${apiUri}/stuff/${id}${mock}`
  };
  return await axiosCallAsync(getMsg);
};

/** @param {Record<string, any>} input */
export const apiCreateStuffAsync = async (input) => {
  const postMsg = {
    method: isMock ? 'get' : 'post',
    headers: {
      'Content-Type': 'application/json',
      RequestVerificationToken: getCsrfToken('RequestVerificationToken')
    },
    url: `${apiUri}/stuff${mock}`,
    data: input
  };
  return await axiosCallAsync(postMsg);
};

/** @param {string} id @param {Record<string, any>} input */
export const apiUpdateStuffAsync = async (id, input) => {
  const putMsg = {
    method: isMock ? 'get' : 'put',
    headers: {
      'Content-Type': 'application/json',
      RequestVerificationToken: getCsrfToken('RequestVerificationToken')
    },
    url: `${apiUri}/stuff/${id}${mock}`,
    data: input
  };
  return await axiosCallAsync(putMsg);
};

/** @param {string} id */
export const apiDeleteStuffAsync = async (id) => {
  const deleteMsg = {
    method: isMock ? 'get' : 'delete',
    headers: {
      'Content-Type': 'application/json',
      RequestVerificationToken: getCsrfToken('RequestVerificationToken')
    },
    url: `${apiUri}/stuff/${id}${mock}`
  };
  return await axiosCallAsync(deleteMsg);
};

/** @param {unknown} error */
const getErrorMsg = (error) => {
  const msg = apiErrMsg.generic;
  if (axios.isAxiosError(error)) {
    if (error.response?.status === 401) {
      return apiErrMsg.unauthorized;
    }

    if (error.response?.data?.detail) {
      return error.response.data.detail;
    }
  }

  return msg;
};

/** @param {string} crudTitle @param {Record<string, any>} item */
export const crudApiCallAsync = async (crudTitle, item) => {
  delete item.error;
  let result;

  if (crudTitle === crud.READ) {
    goto(resolve('/'));
    return item;
  }

  if (crudTitle === crud.CREATE) {
    result = await apiCreateStuffAsync(item);
  }

  if (crudTitle === crud.UPDATE) {
    result = await apiUpdateStuffAsync(item.id, item);
  }

  if (crudTitle === crud.DELETE) {
    result = await apiDeleteStuffAsync(item.id);
  }

  if (result.error) {
    item.error = result.error;
    return item;
  }

  goto(resolve('/'));
  return item;
};

/** @param {string} crudTitle @param {string} id */
export const getItemAsync = async (crudTitle, id) => {
  /** @type {Record<string, any>} */
  let item = {};
  /** @type {Record<string, any>} */
  const initialItem = {};

  if (crudTitle !== crud.CREATE) {
    item = await apiGetStuffByIdAsync(id);
  }

  for (let key in item) {
    initialItem[key] = item[key];
  }

  return { item, initialItem };
};
