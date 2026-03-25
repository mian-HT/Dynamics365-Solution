package com.xgate.d365proxy.controller;

import com.xgate.d365proxy.service.D365PushService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

@Slf4j
@RestController
@RequestMapping("/api/sms")
@RequiredArgsConstructor
public class SmsCallbackController {

    private final D365PushService d365PushService;

    /**
     * 短信状态回执回调接口
     * GET /api/sms/callback/{tenantId}?messageID=xxx&externalId=xxx&state=xxx&...
     */
    @GetMapping("/callback/{tenantId}")
    public String callback(@PathVariable String tenantId,
                           @RequestParam Map<String, String> queryParams) {
        log.info("Received SMS callback for tenant: {}, params: {}", tenantId, queryParams);
        try {
            d365PushService.processCallback(tenantId, queryParams);
        } catch (Exception e) {
            log.error("Failed to process SMS callback for tenant: {}", tenantId, e);
        }
        return "success";
    }
}
