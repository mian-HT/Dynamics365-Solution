package com.xgate.d365proxy.service.impl;

import com.xgate.d365proxy.service.OAuthTokenService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.cache.annotation.CacheEvict;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.util.LinkedMultiValueMap;
import org.springframework.util.MultiValueMap;
import org.springframework.web.client.RestTemplate;

import java.util.Map;

@Slf4j
@Service
@RequiredArgsConstructor
public class OAuthTokenServiceImpl implements OAuthTokenService {

    private static final String TOKEN_URL_TEMPLATE =
            "https://login.microsoftonline.com/%s/oauth2/v2.0/token";

    private final RestTemplate restTemplate;

    @Override
    @Cacheable(value = "oauthToken", key = "#tenantId")
    public String getAccessToken(String tenantId, String clientId,
                                 String clientSecret, String d365InstanceUrl) {
        String tokenUrl = String.format(TOKEN_URL_TEMPLATE, tenantId);
        String scope = d365InstanceUrl + "/.default";

        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_FORM_URLENCODED);

        MultiValueMap<String, String> formData = new LinkedMultiValueMap<>();
        formData.add("client_id", clientId);
        formData.add("client_secret", clientSecret);
        formData.add("grant_type", "client_credentials");
        formData.add("scope", scope);

        log.info("Requesting OAuth token for tenant: {}", tenantId);

        HttpEntity<MultiValueMap<String, String>> request = new HttpEntity<>(formData, headers);
        @SuppressWarnings("unchecked")
        ResponseEntity<Map<String, Object>> response = restTemplate.postForEntity(
                tokenUrl, request, (Class<Map<String, Object>>) (Class<?>) Map.class);

        Map<String, Object> body = response.getBody();
        String accessToken = body != null ? (String) body.get("access_token") : null;

        if (accessToken == null || accessToken.isEmpty()) {
            log.error("Failed to obtain OAuth token for tenant: {}, response: {}", tenantId, body);
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
