package com.xgate.d365proxy.service;

import java.util.Map;

public interface D365PushService {

    /**
     * 核心流程：查询租户配置 -> 获取Token -> 组装payload -> 推送到D365
     *
     * @param tenantId    URL路径中的租户ID
     * @param queryParams 短信回执的所有 query 参数
     */
    void processCallback(String tenantId, Map<String, String> queryParams);
}
