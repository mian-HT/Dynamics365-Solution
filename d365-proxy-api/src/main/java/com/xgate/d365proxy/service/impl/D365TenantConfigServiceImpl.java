package com.xgate.d365proxy.service.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.xgate.d365proxy.entity.D365TenantConfig;
import com.xgate.d365proxy.mapper.D365TenantConfigMapper;
import com.xgate.d365proxy.service.D365TenantConfigService;
import org.springframework.stereotype.Service;

@Service
public class D365TenantConfigServiceImpl
        extends ServiceImpl<D365TenantConfigMapper, D365TenantConfig>
        implements D365TenantConfigService {

    @Override
    public D365TenantConfig getActiveByTenantId(String tenantId) {
        return getOne(new LambdaQueryWrapper<D365TenantConfig>()
                .eq(D365TenantConfig::getTenantId, tenantId)
                .eq(D365TenantConfig::getIsActive, 1));
    }
}
