package com.xgate.d365proxy.service;

import com.baomidou.mybatisplus.extension.service.IService;
import com.xgate.d365proxy.entity.D365TenantConfig;

public interface D365TenantConfigService extends IService<D365TenantConfig> {

    /**
     * 根据 tenantId 查询启用状态的租户配置
     */
    D365TenantConfig getActiveByTenantId(String tenantId);
}
