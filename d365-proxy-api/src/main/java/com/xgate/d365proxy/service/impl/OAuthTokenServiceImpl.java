package com.xgate.d365proxy.service.impl;

import cn.hutool.http.HttpRequest;
import cn.hutool.json.JSONObject;
import cn.hutool.json.JSONUtil;
import com.xgate.d365proxy.service.OAuthTokenService;
import lombok.extern.slf4j.Slf4j;
import org.springframework.cache.annotation.CacheEvict;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.stereotype.Service;

@Slf4j
@Service
public class OAuthTokenServiceImpl implements OAuthTokenService {

    private static final String TOKEN_URL_TEMPLATE =
            "https://login.microsoftonline.com/%s/oauth2/v2.0/token";

    @Override
    @Cacheable(value = "oauthToken", key = "#tenantId")
    public String getAccessToken(String tenantId, String clientId,
                                 String clientSecret, String d365InstanceUrl) {
        String tokenUrl = String.format(TOKEN_URL_TEMPLATE, tenantId);
        String scope = d365InstanceUrl + "/.default";

        String formBody = "client_id=" + clientId
                + "&client_secret=" + clientSecret
                + "&grant_type=client_credentials"
                + "&scope=" + scope;

        log.info("Requesting OAuth token for tenant: {}", tenantId);

        String responseBody = HttpRequest.post(tokenUrl)
                .header("Content-Type", "application/x-www-form-urlencoded")
                .body(formBody)
                .timeout(10000)
                .execute()
                .body();

        JSONObject json = JSONUtil.parseObj(responseBody);
        String accessToken = json.getStr("access_token");

        if (accessToken == null || accessToken.isEmpty()) {
            log.error("Failed to obtain OAuth token for tenant: {}, response: {}", tenantId, responseBody);
            throw new RuntimeException("Failed to obtain OAuth token for tenant: " + tenantId);
        }

        log.info("OAuth token obtained successfully for tenant: {}", tenantId);
        return accessToken;
    }

    @Override
    @CacheEvict(value = "oauthToken", key = "#tenantId")
    public void evictToken(String tenantId) {
        log.info("Token cache evicted for tenant: {}", tenantId);
    }
}
