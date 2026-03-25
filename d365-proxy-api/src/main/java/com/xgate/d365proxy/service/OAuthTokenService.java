package com.xgate.d365proxy.service;

public interface OAuthTokenService {

    /**
     * 获取指定租户的 OAuth 2.0 Access Token（带 Caffeine 缓存）
     */
    String getAccessToken(String tenantId, String clientId, String clientSecret, String d365InstanceUrl);

    /**
     * 手动清除指定租户的 Token 缓存
     */
    void evictToken(String tenantId);
}
