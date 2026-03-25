package com.xgate.d365proxy.entity;

import com.baomidou.mybatisplus.annotation.*;
import lombok.Data;

import java.time.LocalDateTime;

@Data
@TableName("d365_tenant_config")
public class D365TenantConfig {

    @TableId(type = IdType.AUTO)
    private Long id;

    private String tenantId;

    private String clientId;

    private String clientSecret;

    private String d365InstanceUrl;

    private Integer isActive;

    @TableField(fill = FieldFill.INSERT)
    private LocalDateTime createTime;

    @TableField(fill = FieldFill.INSERT_UPDATE)
    private LocalDateTime updateTime;
}
