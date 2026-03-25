package com.xgate.d365proxy.service.impl;

import cn.hutool.http.HttpRequest;
import cn.hutool.http.HttpResponse;
import cn.hutool.json.JSONUtil;
import com.xgate.d365proxy.entity.D365TenantConfig;
import com.xgate.d365proxy.service.D365PushService;
import com.xgate.d365proxy.service.D365TenantConfigService;
import com.xgate.d365proxy.service.OAuthTokenService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import java.util.Map;
import java.util.stream.Collectors;

@Slf4j
@Service
@RequiredArgsConstructor
public class D365PushServiceImpl implements D365PushService {

    private static final String D365_API_PATH = "/api/data/v9.2/xgate_SmsDeliveryReportCustomApi";

    private final D365TenantConfigService tenantConfigService;
    private final OAuthTokenService oAuthTokenService;

    @Override
    public void processCallback(String tenantId, Map<String, String> queryParams) {
        // 1. 查询租户配置
        D365TenantConfig config = tenantConfigService.getActiveByTenantId(tenantId);
        if (config == null) {
            throw new RuntimeException("Tenant config not found or inactive: " + tenantId);
        }

        // 2. 获取 OAuth Token（带缓存）
        String accessToken = oAuthTokenService.getAccessToken(
                tenantId,
                config.getClientId(),
                config.getClientSecret(),
                config.getD365InstanceUrl());

        // 3. 组装 payload — 将所有 query 参数拼接为 key=value&key=value 格式
        String payload = queryParams.entrySet().stream()
                .map(e -> e.getKey() + "=" + e.getValue())
                .collect(Collectors.joining("&"));

        String requestBody = JSONUtil.createObj()
                .set("payload", payload)
                .toString();

        // 4. 推送到 D365
        String d365Url = config.getD365InstanceUrl() + D365_API_PATH;
        log.info("Pushing delivery receipt to D365: {} | payload length: {}", d365Url, payload.length());

        try {
            HttpResponse response = HttpRequest.post(d365Url)
                    .header("Authorization", "Bearer " + accessToken)
                    .header("Content-Type", "application/json")
                    .body(requestBody)
                    .timeout(15000)
                    .execute();

            int status = response.getStatus();
            if (status == 200 || status == 204) {
                log.info("D365 push success for tenant: {}, HTTP status: {}", tenantId, status);
            } else {
                log.error("D365 push failed for tenant: {}, HTTP status: {}, body: {}",
                        tenantId, status, response.body());
            }
        } catch (Exception e) {
            log.error("D365 push exception for tenant: {}", tenantId, e);
            throw e;
        }
    }
}
