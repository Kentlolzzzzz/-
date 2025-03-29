/**
 * 财务管理系统 - 侧边栏导航组件
 * 实现可折叠式导航菜单，便于在多个页面复用
 */

// 导航菜单数据
const navData = [
    {
        id: 'system',
        title: '系统功能',
        items: [
            { title: '功能总览', icon: 'dashboard', url: 'index.html' },
            { title: '基础设置', icon: 'settings', url: 'basic-info.html' }
        ]
    },
    {
        id: 'data-entry',
        title: '数据录入',
        items: [
            { title: '收支登记', icon: 'book', url: 'income-expense-register.html' },
            { title: '内部转账', icon: 'swap_horiz', url: 'internal-transfer.html' },
            { title: '应收', icon: 'arrow_downward', url: 'accounts-receivable.html' },
            { title: '应付', icon: 'arrow_upward', url: 'accounts-payable.html' },
            { title: '借款', icon: 'account_balance_wallet', url: 'employee-loan.html' }
        ]
    },
    {
        id: 'invoice-entry',
        title: '票据录入',
        items: [
            { title: '采购合同', icon: 'shopping_cart', url: 'purchase-contract.html' },
            { title: '销售合同', icon: 'store', url: 'sales-contract.html' },
            { title: '销项发票登记', icon: 'receipt', url: 'sales-invoice-register.html' },
            { title: '进项发票登记', icon: 'description', url: 'purchase-invoice-register.html' },
            { title: '工资', icon: 'monetization_on', url: 'staff-salary.html' },
            { title: '考勤表', icon: 'event', url: 'attendance-sheet.html' }
        ]
    },
    {
        id: 'data-summary',
        title: '数据汇总',
        items: [
            { title: '工程项目明细', icon: 'business', url: 'project-detail.html' },
            { title: '任意时段表', icon: 'date_range', url: 'period-report.html' },
            { title: '账户统计', icon: 'account_balance', url: 'account-statistics.html' },
            { title: '员工统计', icon: 'people', url: 'employee-statistics.html' },
            { title: '项目统计', icon: 'trending_up', url: 'project-statistics.html' },
            { title: '往来报表', icon: 'compare_arrows', url: 'transaction-report.html' }
        ]
    },
    {
        id: 'data-report',
        title: '数据报表',
        items: [
            { title: '成本核算明细', icon: 'attach_money', url: 'cost-detail.html' },
            { title: '收入核算明细', icon: 'timeline', url: 'income-detail.html' },
            { title: '收入汇总表', icon: 'bar_chart', url: 'income-summary.html' },
            { title: '支出汇总表', icon: 'pie_chart', url: 'expense-summary.html' },
            { title: '利润表', icon: 'assessment', url: 'profit-report.html' }
        ]
    }
];

/**
 * 初始化侧边栏导航
 */
function initSidebar() {
    const sidebar = document.getElementById('sidebar');
    if (!sidebar) return;
    
    // 添加logo
    const logoDiv = document.createElement('div');
    logoDiv.className = 'logo';
    logoDiv.innerHTML = `
        <i class="material-icons">account_balance</i>
        <span>财务管理系统</span>
    `;
    sidebar.appendChild(logoDiv);
    
    // 获取当前页面路径
    const currentPagePath = window.location.pathname;
    const currentPageName = currentPagePath.split('/').pop() || 'index.html';
    
    // 渲染导航菜单
    navData.forEach(section => {
        const sectionDiv = document.createElement('div');
        sectionDiv.className = 'nav-section';
        sectionDiv.id = section.id;
        
        // 创建分类标题
        const titleDiv = document.createElement('div');
        titleDiv.className = 'nav-section-title';
        titleDiv.innerHTML = `
            <span>${section.title}</span>
            <i class="material-icons toggle-icon">keyboard_arrow_down</i>
        `;
        
        // 创建菜单项容器
        const itemsDiv = document.createElement('div');
        itemsDiv.className = 'nav-items';
        
        // 添加菜单项
        section.items.forEach(item => {
            const isActive = item.url === currentPageName;
            const itemLink = document.createElement('a');
            itemLink.href = item.url;
            itemLink.className = `nav-item${isActive ? ' active' : ''}`;
            itemLink.innerHTML = `
                <i class="material-icons">${item.icon}</i>
                <span>${item.title}</span>
            `;
            itemsDiv.appendChild(itemLink);
        });
        
        // 组装菜单区块
        sectionDiv.appendChild(titleDiv);
        sectionDiv.appendChild(itemsDiv);
        sidebar.appendChild(sectionDiv);
        
        // 添加点击事件
        titleDiv.addEventListener('click', () => {
            titleDiv.classList.toggle('collapsed');
            itemsDiv.classList.toggle('collapsed');
            
            // 保存折叠状态到本地存储
            saveCollapseState();
        });
    });
    
    // 恢复折叠状态
    restoreCollapseState();
}

/**
 * 保存菜单折叠状态到本地存储
 */
function saveCollapseState() {
    const collapseState = {};
    document.querySelectorAll('.nav-section').forEach(section => {
        const sectionId = section.id;
        const isCollapsed = section.querySelector('.nav-section-title').classList.contains('collapsed');
        collapseState[sectionId] = isCollapsed;
    });
    
    localStorage.setItem('sidebarCollapseState', JSON.stringify(collapseState));
}

/**
 * 从本地存储恢复菜单折叠状态
 */
function restoreCollapseState() {
    try {
        const collapseState = JSON.parse(localStorage.getItem('sidebarCollapseState'));
        if (!collapseState) return;
        
        // 应用保存的折叠状态
        Object.keys(collapseState).forEach(sectionId => {
            const section = document.getElementById(sectionId);
            if (!section) return;
            
            const titleDiv = section.querySelector('.nav-section-title');
            const itemsDiv = section.querySelector('.nav-items');
            
            if (collapseState[sectionId]) {
                titleDiv.classList.add('collapsed');
                itemsDiv.classList.add('collapsed');
            } else {
                titleDiv.classList.remove('collapsed');
                itemsDiv.classList.remove('collapsed');
            }
        });
    } catch (error) {
        console.error('恢复侧边栏状态失败:', error);
    }
} 