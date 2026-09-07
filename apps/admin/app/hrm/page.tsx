import { ModulePage } from '../../components/module-page';

const features = [
  { name: 'Employees directory', endpoint: '/hrm/employees', action: 'list/create/edit tabs with masked bank fields' },
  { name: 'Departments', endpoint: '/hrm/departments', action: 'manage lookups' },
  { name: 'Jobs', endpoint: '/hrm/jobs', action: 'manage lookups' },
  { name: 'Attendance import', endpoint: '/hrm/attendance/import', action: 'CSV machine/enroll/datetime/inout' },
  { name: 'Working hours summary', endpoint: '/hrm/attendance/summary', action: 'naive in/out pairing' },
  { name: 'Salary adjustments', endpoint: '/hrm/adjustments', action: 'addition/deduction approval' },
  { name: 'Payroll wizard', endpoint: '/hrm/payroll/runs', action: 'preview/create/post/pay/reverse' },
  { name: 'Payslip print', endpoint: '/hrm/payroll/runs/{id}', action: 'HTML payslip per employee' },
];

export default function Page() { return <ModulePage title="الموارد البشرية والرواتب" subtitle="ملفات الموظفين والحضور والتعديلات ومسير الرواتب وقسائم الراتب." features={features} kind="hrm" />; }
