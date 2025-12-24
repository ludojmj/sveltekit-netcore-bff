// api.js
import { goto } from '$app/navigation';
import axios from 'axios';
axios.defaults.withCredentials = true;
import { isLoading, userInfo } from '$lib/store.js';
import { apiErrMsg, crud } from '$lib/const.js';

const rootApi = import.meta.env.VITE_API_URL;
const isSelfHostedClient = rootApi.indexOf('http') < 0;
const apiUri = isSelfHostedClient ? `${window.location.origin}/${rootApi}` : `${rootApi}`;
const isMock = apiUri.indexOf('mock') > -1;

const getCsrfToken = (cookieName) => {
  const regex = new RegExp(`(^| )${cookieName}=([^;]+)`);
  const match = document.cookie.match(regex);
  return match ? decodeURIComponent(match[2]) : '';
};

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
  const mock = isMock ? '.json' : '';
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
  window.location = apiUri.replace('api', '');
};

export const apiLogoutAsync = async () => {
  const getMsg = {
    method: 'get',
    url: `${apiUri}/auth/logout`
  };
  const result = await axiosCallAsync(getMsg);
  window.location = 'https://demo.duendesoftware.com/connect/endsession?id_token_hint=' +
    result +
    '&post_logout_redirect_uri=' +
    encodeURIComponent(window.location.origin + '/');
};

export const apiSearchStuffAsync = async (search) => {
  const mock = isMock ? '.json' : '';
  const getMsg = {
    method: 'get',
    url: `${apiUri}/stuff${mock}?search=${search}`
  };
  return await axiosCallAsync(getMsg);
};

export const apiGetStuffListAsync = async () => {
  const mock = isMock ? '.json' : '';
  const getMsg = {
    method: 'get',
    url: `${apiUri}/stuff${mock}`
  };
  return await axiosCallAsync(getMsg);
};

export const apiGotoPageAsync = async (page) => {
  const mock = isMock ? `${page}.json` : '';
  const getMsg = {
    method: 'get',
    url: `${apiUri}/stuff${mock}?page=${page}`
  };
  return await axiosCallAsync(getMsg);
};

export const apiGetStuffByIdAsync = async (id) => {
  const mock = isMock ? '.json' : '';
  const getMsg = {
    method: 'get',
    url: `${apiUri}/stuff/${id}${mock}`
  };
  return await axiosCallAsync(getMsg);
};

export const apiCreateStuffAsync = async (input) => {
  const mock = isMock ? '.json' : '';
  const postMsg = {
    method: isMock ? 'get' : 'post',
    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getCsrfToken('RequestVerificationToken') },
    url: `${apiUri}/stuff${mock}`,
    data: input
  };
  return await axiosCallAsync(postMsg);
};

export const apiUpdateStuffAsync = async (id, input) => {
  const mock = isMock ? '.json' : '';
  const putMsg = {
    method: isMock ? 'get' : 'put',
    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getCsrfToken('RequestVerificationToken') },
    url: `${apiUri}/stuff/${id}${mock}`,
    data: input
  };
  return await axiosCallAsync(putMsg);
};

export const apiDeleteStuffAsync = async (id) => {
  const mock = isMock ? '.json' : '';
  const deleteMsg = {
    method: isMock ? 'get' : 'delete',
    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getCsrfToken('RequestVerificationToken') },
    url: `${apiUri}/stuff/${id}${mock}`
  };
  return await axiosCallAsync(deleteMsg);
};

const getErrorMsg = (error) => {
  const msg = apiErrMsg.generic;
  if (error.response?.status === 401) {
    return apiErrMsg.unauthorized;
  }

  if (error.response?.data?.detail) {
    return error.response.data.detail;
  }

  return msg;
};

export const crudApiCallAsync = async (crudTitle, item) => {
  delete item.error;
  let result;

  if (crudTitle === crud.READ) {
    goto('/');
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

  goto('/');
  return item;
};

export const getItemAsync = async (crudTitle, id) => {
  let item = {};
  const initialItem = {};

  if (crudTitle !== crud.CREATE) {
    item = await apiGetStuffByIdAsync(id);
  }

  for (let key in item) {
    initialItem[key] = item[key];
  }

  return { item, initialItem };
};
