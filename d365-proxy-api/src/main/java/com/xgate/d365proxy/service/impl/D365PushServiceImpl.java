package com.xgate.d365proxy.service.impl;

import com.xgate.d365proxy.entity.D365TenantConfig;
import com.xgate.d365proxy.service.D365PushService;
import com.xgate.d365proxy.service.D365TenantConfigService;
import com.xgate.d365proxy.service.OAuthTokenService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestTemplate;

import java.util.Map;
import java.util.stream.Collectors;

@Slf4j
@Service
@RequiredArgsConstructor
public class D365PushServiceImpl implements D365PushService {

    private static final String D365_API_PATH = "/api/data/v9.2/xgate_SmsDeliveryReportCustomApi";

    private final D365TenantConfigService tenantConfigService;
    private final OAuthTokenService oAuthTokenService;
    private final RestTemplate restTemplate;

    @Override
    public void processCallback(String tenantId, Map<String, String> queryParams) {
        D365TenantConfig config = tenantConfigService.getActiveByTenantId(tenantId);
        if (config == null) {
            throw new RuntimeException("Tenant config not found or inactive: " + tenantId);
        }

        String accessToken = oAuthTokenService.getAccessToken(
                tenantId,
                config.getClientId(),
                config.getClientSecret(),
                config.getD365InstanceUrl());

        String payload = queryParams.entrySet().stream()
                .map(e -> e.getKey() + "=" + e.getValue())
                .collect(Collectors.joining("&"));

        String d365Url = config.getD365InstanceUrl() + D365_API_PATH;
        log.info("Pushing delivery receipt to D365: {} | payload length: {}", d365Url, payload.length());

        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_JSON);
        headers.setBearerAuth(accessToken);

        HttpEntity<Map<String, String>> request = new HttpEntity<>(Map.of("payload", payload), headers);

        try {
            ResponseEntity<String> response = restTemplate.postForEntity(d365Url, request, String.class);

            int status = response.getStatusCode().value();
            if (status == 200 || status == 204) {
                log.info("D365 push success for tenant: {}, HTTP status: {}", tenantId, status);
            } else {
                log.error("D365 push failed for tenant: {}, HTTP status: {}, body: {}",
                        tenantId, status, response.getBody());
            }
        } catch (Exception e) {
            log.error("D365 push exception for tenant: {}", tenantId, e);
            throw e;
        }
    }
}
