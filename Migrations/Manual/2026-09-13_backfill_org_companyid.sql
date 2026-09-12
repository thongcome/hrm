-- com_organization.companyid was never populated (all 173 rows NULL) even though
-- the whole tree has only ever belonged to one real company (ADVD, com_company.id=3).
-- CEO, 13 ก.ย. 2569: pointed out this ambiguity is real "มั่ว" given several other
-- company rows exist (PTEST/PTEST2/PTEST3 test data, AD inactive) — backfill the
-- existing real tree to the one real, active company it has always been.
-- Idempotent: only touches rows still NULL.
UPDATE com_organization
SET companyid = (SELECT id FROM com_company WHERE code = 'ADVD')
WHERE companyid IS NULL;
